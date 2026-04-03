using System.DirectoryServices.Protocols;
using Microsoft.Extensions.Logging;
using Multifactor.Core.Ldap;
using Multifactor.Core.Ldap.Connection;
using Multifactor.Core.Ldap.Connection.LdapConnectionFactory;
using Multifactor.Core.Ldap.Schema;
using Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.LoadLdapForest.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.LoadLdapForest.Port;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Features.PacketHandler.UseCases.LoadLdapForest;

internal sealed class LoadLdapForest : ILoadLdapForest
{
    private readonly ILdapConnectionFactory _connectionFactory;
    private readonly LdapSchemaLoader _schemaLoader;
    private readonly ILogger<ILoadLdapForest> _logger;

    public LoadLdapForest(ILdapConnectionFactory connectionFactory,
        ILogger<ILoadLdapForest> logger, LdapSchemaLoader schemaLoader)
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
        try
        {
            _logger.LogDebug("Loading forest metadata from {connectionString}", dto.ConnectionString);

            var ldapConnectionString = new LdapConnectionString(dto.ConnectionString, true);
            using var connection = CreateConnection(ldapConnectionString, dto.UserName, 
                dto.Password, dto.BindTimeoutInSeconds);

            var (rootDn, domain) = GetDomainName(connection);

            _logger.LogDebug("Root domain: {domain}, Root DN: {rootDn}", domain, rootDn);

            var metadata = new ForestMetadata();
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
            var rootDseRequest = new SearchRequest(
                "",
                "(objectClass=*)",
                SearchScope.Base,
                "defaultNamingContext", "dnsHostName");

            var response = (SearchResponse)connection.SendRequest(rootDseRequest);

            if (response.Entries.Count > 0)
            {
                var entry = response.Entries[0];

                // Получаем defaultNamingContext (DN корневого домена)
                if (entry.Attributes["defaultNamingContext"] != null)
                {
                    rootDn = entry.Attributes["defaultNamingContext"][0].ToString();
                    dnsName = ExtractDnsFromDn(rootDn);
                    _logger.LogDebug("Detected domain from RootDSE: {domain} (DN: {dn})", dnsName, rootDn);
                }
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

    private List<DomainInfo> GetTrustedDomains(ILdapConnection connection, string rootDn, LdapConnectionString ldapConnectionString, string userName, string password, int bindTimeoutInSeconds)
    {
        var trustedDomains = new List<DomainInfo>();
        try
        {
            var searchRequest = new SearchRequest(
                $"{DomainLocation},{rootDn}",
                $"(objectClass={DomainObjectClass})",
                SearchScope.OneLevel,
                "cn", "trustPartner", "trustDirection", "trustType", "trustAttributes");

            var response = (SearchResponse)connection.SendRequest(searchRequest);

            foreach (SearchResultEntry entry in response.Entries)
            {
                try
                {
                    var netBiosName = GetAttributeValue(entry, "cn");
                    var dnsName = GetAttributeValue(entry, "trustPartner");

                    // Если нет trustPartner, используем cn как DNS имя
                    if (string.IsNullOrEmpty(dnsName))
                    {
                        dnsName = netBiosName;
                    }

                    var dn = ConvertToDn(dnsName);

                    var connectionString = BuildConnectionStringForDomain(ldapConnectionString, dnsName);
                    var schema = LoadSchema(connectionString, userName, password, bindTimeoutInSeconds);

                    var domainInfo = new DomainInfo
                    {
                        ConnectionString = connectionString,
                        NetBiosName = netBiosName,
                        DnsName = dnsName,
                        DistinguishedName = dn,
                        Schema = schema
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
            var configDn = $"{SuffixLocation},{domainDn}";

            var searchRequest = new SearchRequest(
                configDn,
                "(objectClass=*)",
                SearchScope.Base,
                SuffixAttribute);

            var response = (SearchResponse)connection.SendRequest(searchRequest);

            if (response.Entries.Count > 0)
            {
                var entry = response.Entries[0];
                if (entry.Attributes.Contains("uPNSuffixes"))
                {
                    var values = entry.Attributes["uPNSuffixes"].GetValues(typeof(string));
                    if (values != null)
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
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "No UPN suffixes found for {domainDn} (normal for child domains)", domainDn);
            // Не логируем как ошибку - для дочерних доменов это нормально
        }

        return suffixes;
    }
    
    private ILdapSchema LoadSchema(string connectionString, string username, string password, int bindTimeoutInSeconds)
    {
        var options = new LdapConnectionOptions(
            new LdapConnectionString(connectionString, true),
            AuthType.Negotiate,
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

    private static string? GetAttributeValue(SearchResultEntry entry, string attributeName)
    {
        if (entry.Attributes.Contains(attributeName))
        {
            var values = entry.Attributes[attributeName].GetValues(typeof(string));
            if (values is { Length: > 0 })
            {
                return values[0].ToString();
            }
        }
        return null;
    }
    
    private static string BuildConnectionStringForDomain(LdapConnectionString baseConnectionString, string domainDnsName)
    {
        return new LdapConnectionString($"{baseConnectionString.Scheme}://{domainDnsName}:{baseConnectionString.Port}").ToString()!;
    }
}


