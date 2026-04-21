using Elastic.CommonSchema;
using Microsoft.Extensions.Logging;
using Multifactor.Core.Ldap;
using Multifactor.Core.Ldap.Connection;
using Multifactor.Core.Ldap.Connection.LdapConnectionFactory;
using Multifactor.Core.Ldap.Name;
using Multifactor.Core.Ldap.Schema;
using Multifactor.Core.Ldap.Extensions;
using Multifactor.Radius.Adapter.v2.Application.Core.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.LoadLdapForest.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.LoadLdapForest.Port;
using System.DirectoryServices.Protocols;
using Multifactor.Core.Ldap.Entry;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Features.PacketHandler.UseCases.LoadLdapForest;

internal sealed class LoadLdapForest : ILoadLdapForest
{
    private readonly ILdapConnectionFactory _connectionFactory;
    private readonly LdapSchemaLoader _schemaLoader;
    private readonly ILogger<ILoadLdapForest> _logger;

    public LoadLdapForest(ILdapConnectionFactory connectionFactory, LdapSchemaLoader schemaLoader,
        ILogger<ILoadLdapForest> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
        _schemaLoader = schemaLoader;
    }

    private const string DomainLocation = "CN=System";
    private const string DomainObjectClass = "trustedDomain";
    private const string SuffixLocation = "CN=Partitions,CN=Configuration";
    private const string SuffixAttribute = "uPNSuffixes";

    /// <summary>
    /// Загружает метаданные леса Active Directory
    /// </summary>
    public IForestMetadata? Execute(LoadMetadataDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        try
        {
            _logger.LogDebug("Loading forest metadata from {connectionString}", dto.ConnectionString);

            var ldapConnectionString = new LdapConnectionString(dto.ConnectionString);
            using var connection = CreateConnection(ldapConnectionString, dto.UserName, 
                dto.Password, dto.BindTimeoutInSeconds);

            var (rootDn, domain) = GetDomainName(connection);

            _logger.LogDebug("Root domain: {domain}, Root DN: {rootDn}", domain, rootDn);

            var metadata = new ForestMetadata();

            var rootDomainInfo = GetDomainInfo(connection, rootDn, ldapConnectionString, domain, dto.UserName,
                dto.Password, dto.BindTimeoutInSeconds);
            if (rootDomainInfo != null)
            {
                AddDomainToMetadata(metadata, rootDomainInfo);
                _logger.LogDebug("Added root domain: {domain} (DN: {dn})",
                    rootDomainInfo.DnsName, rootDomainInfo.DistinguishedName);
            }

            var trustedDomains = GetTrustedDomains(connection, rootDn, ldapConnectionString,  dto.UserName, 
                dto.Password, dto.BindTimeoutInSeconds);
            foreach (var trustedDomain in trustedDomains)
            {
                AddDomainToMetadata(metadata, trustedDomain);
                _logger.LogDebug("Added trusted domain: {domain} (DN: {dn})",
                    trustedDomain.DnsName, trustedDomain.DistinguishedName);
            }

            foreach (var domainInfo in metadata.Domains.Values.ToList())
            {
                var suffixes = GetUpnSuffixes(connection, domainInfo.DistinguishedName, dto.AlternativeSuffixesEnabled);
                foreach (var suffix in suffixes.Where(suffix => !metadata.UpnSuffixes.ContainsKey(suffix)))
                {
                    metadata.UpnSuffixes[suffix] = domainInfo;
                    domainInfo.UpnSuffixes.Add(suffix);
                    _logger.LogDebug("UPN suffix '{suffix}' -> domain '{domain}'",
                        suffix, domainInfo.DnsName);
                }

                if (metadata.UpnSuffixes.TryAdd(domainInfo.DnsName, domainInfo))
                {
                    domainInfo.UpnSuffixes.Add(domainInfo.DnsName);
                }
            }

            _logger.LogInformation(
                "Forest metadata loaded: {domainCount} domains, {suffixCount} UPN suffixes",
                metadata.Domains.Count,
                metadata.UpnSuffixes.Count);

            return metadata;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load forest metadata from {connectionString}",
                dto.ConnectionString);
            return null;
        }
    }
    
    private (string dn, string dns) GetDomainName(ILdapConnection connection)
    {
        string? rootDn = null;
        string? dnsName = null;

        try
        {
            var rootDse = connection.FindOne(
                DistinguishedName.Empty,
                "(objectClass=*)",
                SearchScope.Base,
                WellKnownAttributes.DefaultNamingContext,
                WellKnownAttributes.DnsHostName);


            rootDn = GetAttributeValue(rootDse, WellKnownAttributes.DefaultNamingContext);
            if (rootDn is not null)
            {
                dnsName = ExtractDnsFromDn(rootDn);
                _logger.LogDebug("Detected domain from RootDSE: {domain} (DN: {dn})", dnsName, rootDn);
            }
            return (rootDn, dnsName);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not get domain info from RootDSE");
            return default;
        }
    }

    private ILdapConnection CreateConnection(LdapConnectionString connectionString, string userName, string password, int bindTimeoutInSeconds)
    {
        var options = new LdapConnectionOptions(connectionString, 
            AuthType.Basic, 
            userName, 
            password, 
            TimeSpan.FromSeconds(bindTimeoutInSeconds));
        return _connectionFactory.CreateConnection(options);
    }

    private static string ConvertToDn(string domain)
    {
        var parts = domain.Split('.');
        return string.Join(",", parts.Select(p => $"DC={p}"));
    }

    private static string ExtractDnsFromDn(string dn)
    {
        var parts = dn.Split(',')
            .Where(p => p.Trim().StartsWith("DC=", StringComparison.OrdinalIgnoreCase))
            .Select(p => p.Substring(3).Trim())
            .ToArray();

        return string.Join(".", parts);
    }

    private DomainInfo? GetDomainInfo(ILdapConnection connection, string dn, LdapConnectionString ldapConnectionString, string dnsName, string userName, string password, int bindTimeoutInSeconds)
    {
        try
        {
            var entry = connection.FindOne(
                DistinguishedName.Empty,
                "(objectClass=domain)",
                SearchScope.Base,
                WellKnownAttributes.DefaultNamingContext,
                WellKnownAttributes.DnsHostName,
                "netbiosname");

            if (entry is null)
                return null;

            var netBiosName = GetAttributeValue(entry, "netbiosname");

            if (string.IsNullOrEmpty(netBiosName))
            {
                netBiosName = dnsName.Split('.')[0].ToUpperInvariant();
            }
            var connectionString = BuildConnectionStringForDomain(ldapConnectionString, dnsName);
            var schema = LoadSchema(connectionString, userName, password, bindTimeoutInSeconds, AuthType.Basic);

            return new DomainInfo
            {
                ConnectionString = connectionString,
                DnsName = dnsName,
                DistinguishedName = dn,
                NetBiosName = netBiosName,
                Schema = schema,
                IsTrusted = false
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get domain info for {dn}", dn);
            return null;
        }
    }

    private List<DomainInfo> GetTrustedDomains(ILdapConnection connection, string rootDn, LdapConnectionString ldapConnectionString, string userName, string password, int bindTimeoutInSeconds)
    {
        var trustedDomains = new List<DomainInfo>();
        try
        {
            var searchBase = new DistinguishedName($"{DomainLocation},{rootDn}");
            var entries = connection.Find(
                searchBase,
                $"(objectClass={DomainObjectClass})",
                SearchScope.OneLevel,
                null,
                "cn", 
                "trustPartner", 
                "trustDirection", 
                "trustType", 
                "trustAttributes");

            foreach (var entry in entries)
            {
                try
                {
                    var netBiosName = GetAttributeValue(entry, "cn");
                    var dnsName = GetAttributeValue(entry, "trustPartner");

                    if (string.IsNullOrEmpty(dnsName))
                    {
                        dnsName = netBiosName;
                    }
                    var dn = ConvertToDn(dnsName);   
                    

                    var connectionString = BuildConnectionStringForDomain(ldapConnectionString, dnsName);
                    var upn = UserIdentity.TransformDnToUpn(userName);
                    var schema = LoadSchema(connectionString, upn, password, bindTimeoutInSeconds, AuthType.Negotiate);

                    var domainInfo = new DomainInfo
                    {
                        ConnectionString = connectionString,
                        NetBiosName = netBiosName,
                        DnsName = dnsName,
                        DistinguishedName = dn,
                        Schema = schema,
                        IsTrusted = true
                    };

                    trustedDomains.Add(domainInfo);

                    _logger.LogDebug("Found trusted domain: {netBios} -> {dns} (DN: {dn})",
                        netBiosName, dnsName, dn);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse trusted domain entry");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to query trusted domains from {rootDn}", rootDn);
        }

        return trustedDomains;
    }

    private List<string> GetUpnSuffixes(ILdapConnection connection, string domainDn, bool alternativeSuffixesEnabled)
    {
        var suffixes = new List<string> { ExtractDnsFromDn(domainDn) };

        if (!alternativeSuffixesEnabled)
            return suffixes;

        try
        {
            var configDn = new DistinguishedName($"{SuffixLocation},{domainDn}");

            var entry = connection.FindOne(
                configDn,
                "(objectClass=*)",
                SearchScope.Base,
                SuffixAttribute);
            
            if (entry is not null && entry.Attributes.Any(x => string.Equals(x.Name, SuffixAttribute, StringComparison.CurrentCultureIgnoreCase)))
            {
                var values = entry.Attributes.FirstOrDefault(x=> string.Equals(x.Name, "SuffixAttribute", StringComparison.CurrentCultureIgnoreCase))?.Values;
                if (values is not null)
                {
                    foreach (var value in values)
                    {
                        var suffix = value.ToString();
                        if (!string.IsNullOrEmpty(suffix))
                        {
                            suffixes.Add(suffix);
                        }
                    }
                }
            }
            
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "No UPN suffixes found for {domainDn} (normal for child domains)", domainDn);
            // Не логируем как ошибку - для дочерних доменов это нормально
        }
        return suffixes;
    }
    
    private ILdapSchema LoadSchema(string connectionString, string username, string password, int bindTimeoutInSeconds, AuthType auth)
    {
        _logger.LogDebug($"Trying to connect to: {connectionString} by {username}");
        var options = new LdapConnectionOptions(
            new LdapConnectionString(connectionString),
            auth,
            username,
            password,
            TimeSpan.FromSeconds(bindTimeoutInSeconds)
        );
        return _schemaLoader.Load(options);
    }

    private static void AddDomainToMetadata(ForestMetadata metadata, DomainInfo domainInfo)
    {
        metadata.Domains[domainInfo.DnsName.ToLowerInvariant()] = domainInfo;

        if (!string.IsNullOrEmpty(domainInfo.NetBiosName))
        {
            metadata.NetBiosNames[domainInfo.NetBiosName.ToUpperInvariant()] = domainInfo;
        }
    }

    private static string? GetAttributeValue(LdapEntry entry, string attributeName)
    {
        if (entry.Attributes.Any(x => string.Equals(x.Name, attributeName, StringComparison.CurrentCultureIgnoreCase)))
        {
            var values = entry.Attributes[attributeName].Values;
            if (values is { Length: > 0 })
            {
                return values[0].ToString();
            }
        }
        return null;
    }
    
    private static string BuildConnectionStringForDomain(LdapConnectionString baseConnectionString, string domainDnsName)
    {
        return $"{baseConnectionString.Scheme}://{domainDnsName}:{baseConnectionString.Port}";
    }

}


