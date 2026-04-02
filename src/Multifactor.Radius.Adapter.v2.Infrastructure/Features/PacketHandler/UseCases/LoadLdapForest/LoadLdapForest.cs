using System.DirectoryServices.Protocols;
using Microsoft.Extensions.Logging;
using Multifactor.Core.Ldap;
using Multifactor.Core.Ldap.Connection;
using Multifactor.Core.Ldap.Connection.LdapConnectionFactory;
using Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.LoadLdapForest.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.LoadLdapForest.Port;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Features.PacketHandler.UseCases.LoadLdapForest;

internal sealed class LoadLdapForest : ILoadLdapForest
{
    private readonly ILdapConnectionFactory _connectionFactory;
    private readonly ILogger<ILoadLdapForest> _logger;

    public LoadLdapForest(ILdapConnectionFactory connectionFactory,
        ILogger<ILoadLdapForest> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
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

            using var connection = CreateConnection(dto.ConnectionString, dto.UserName, 
                dto.Password, dto.BindTimeoutInSeconds);

            var (rootDn, domain) = GetDomainName(connection);

            _logger.LogDebug("Root domain: {domain}, Root DN: {rootDn}", domain, rootDn);

            var metadata = new ForestMetadata
            {
                RootDomain = domain
            };

            // ШАГ 1: Получаем информацию о корневом домене
            var rootDomainInfo = GetDomainInfo(connection, rootDn, domain, isTrusted: false);
            if (rootDomainInfo != null)
            {
                AddDomainToMetadata(metadata, rootDomainInfo);
                _logger.LogDebug("Added root domain: {domain} (DN: {dn})",
                    rootDomainInfo.DnsName, rootDomainInfo.DistinguishedName);
            }

            // ШАГ 2: Ищем доверенные домены
            var trustedDomains = GetTrustedDomains(connection, rootDn);
            foreach (var trustedDomain in trustedDomains)
            {
                AddDomainToMetadata(metadata, trustedDomain);
                _logger.LogDebug("Added trusted domain: {domain} (DN: {dn})",
                    trustedDomain.DnsName, trustedDomain.DistinguishedName);
            }

            // ШАГ 3: Для каждого домена получаем UPN-суффиксы
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

                // Добавляем основной DNS суффикс, если его еще нет
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

    private ILdapConnection CreateConnection(string connectionString, string userName, string password, int bindTimeoutInSeconds)
    {
        var options = new LdapConnectionOptions(new LdapConnectionString(connectionString, true, false), 
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

    private DomainInfo? GetDomainInfo(ILdapConnection connection, string dn, string dnsName, bool isTrusted)
    {
        try
        {
            var request = new SearchRequest(
                dn,
                "(objectClass=domain)",
                SearchScope.Base,
                "dn", "distinguishedName", "name", "netbiosname");

            var response = (SearchResponse)connection.SendRequest(request);

            if (response.Entries.Count == 0)
                return null;

            var entry = response.Entries[0];
            var netBiosName = GetAttributeValue(entry, "netbiosname");

            if (string.IsNullOrEmpty(netBiosName))
            {
                netBiosName = dnsName.Split('.')[0].ToUpperInvariant();
            }

            return new DomainInfo
            {
                DnsName = dnsName,
                DistinguishedName = dn,
                NetBiosName = netBiosName,
                IsTrusted = isTrusted
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get domain info for {dn}", dn);
            return null;
        }
    }

    private List<DomainInfo> GetTrustedDomains(ILdapConnection connection, string rootDn)
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

                    var domainInfo = new DomainInfo
                    {
                        NetBiosName = netBiosName,
                        DnsName = dnsName,
                        DistinguishedName = dn,
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
            if (values != null && values.Length > 0)
            {
                return values[0].ToString();
            }
        }
        return null;
    }
}


