using Microsoft.Extensions.Logging;
using Multifactor.Core.Ldap;
using Multifactor.Core.Ldap.Connection;
using Multifactor.Core.Ldap.Connection.LdapConnectionFactory;
using Multifactor.Core.Ldap.LdapGroup.Membership;
using Multifactor.Core.Ldap.Schema;
using Multifactor.Radius.Adapter.v2.Application.Features.Ldap.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.LoadLdapForest.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.LoadLdapForest.Port;
using System.DirectoryServices.Protocols;
using System.Net;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Features.LoadLdapForest;

public class LdapForestLoad : ILdapForestLoad
{
    private readonly ILdapConnectionFactory _connectionFactory;

    public LdapForestLoad(ILdapConnectionFactory connectionFactory,
        ILogger<ILdapForestLoad> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    private readonly ILogger<ILdapForestLoad> _logger;
    private readonly string _domainLocation = "cn=System";
    private readonly string _domainObjectClass = "trustedDomain";
    private readonly string _suffixLocation = "cn=Partitions,cn=Configuration";
    private readonly string _suffixAttribute = "uPNSuffixes";

    /// <summary>
    /// Загружает метаданные леса Active Directory
    /// </summary>
    public IForestMetadata? Execute(LoadMetadataDto request)
    {
        var connectionData = request.ConnectionData;
        try
        {
            _logger.LogDebug("Loading forest metadata from {connectionString}", connectionData.ConnectionString);

            // Создаем подключение
            using var connection = CreateConnection(connectionData);

            // Получаем корневой DN из connection string
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
                var suffixes = GetUpnSuffixes(connection, domainInfo.DistinguishedName, request.AlternativeSuffixesEnabled);
                foreach (var suffix in suffixes)
                {
                    if (!metadata.UpnSuffixes.ContainsKey(suffix))
                    {
                        metadata.UpnSuffixes[suffix] = domainInfo;
                        domainInfo.UpnSuffixes.Add(suffix);
                        _logger.LogDebug("UPN suffix '{suffix}' -> domain '{domain}'",
                            suffix, domainInfo.DnsName);
                    }
                }

                // Добавляем основной DNS суффикс, если его еще нет
                if (!metadata.UpnSuffixes.ContainsKey(domainInfo.DnsName))
                {
                    metadata.UpnSuffixes[domainInfo.DnsName] = domainInfo;
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
                connectionData.ConnectionString);
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

    /// <summary>
    /// Создает LDAP подключение
    /// </summary>
    private ILdapConnection CreateConnection(LdapConnectionData data)
    {
        var options = new LdapConnectionOptions(new LdapConnectionString(data.ConnectionString, true, false),
            AuthType.Basic,
            data.UserName,
            data.Password,
            TimeSpan.FromSeconds(data.BindTimeoutInSeconds));
        return _connectionFactory.CreateConnection(options);
    }

    /// <summary>
    /// Преобразует DNS имя в Distinguished Name
    /// </summary>
    private string ConvertToDn(string domain)
    {
        var parts = domain.Split('.');
        return string.Join(",", parts.Select(p => $"DC={p}"));
    }
    private string ExtractDnsFromDn(string dn)
    {
        // Из DC=company,DC=com → company.com
        var parts = dn.Split(',')
            .Where(p => p.Trim().StartsWith("DC=", StringComparison.OrdinalIgnoreCase))
            .Select(p => p.Substring(3).Trim())
            .ToArray();

        return string.Join(".", parts);
    }

    /// <summary>
    /// Получает информацию о домене
    /// </summary>
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

            // Если нет netbiosname, берем первую часть DNS имени
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

    /// <summary>
    /// Получает список доверенных доменов
    /// </summary>
    private List<DomainInfo> GetTrustedDomains(ILdapConnection connection, string rootDn)
    {
        var trustedDomains = new List<DomainInfo>();

        try
        {
            // Ищем объекты trustedDomain в CN=System
            var searchRequest = new SearchRequest(
                $"CN=System,{rootDn}",
                "(objectClass=trustedDomain)",
                SearchScope.OneLevel,
                "cn", "trustPartner", "trustDirection", "trustType", "trustAttributes");

            var response = (SearchResponse)connection.SendRequest(searchRequest);

            foreach (SearchResultEntry entry in response.Entries)
            {
                try
                {
                    // cn = NetBIOS имя доверенного домена
                    var netBiosName = GetAttributeValue(entry, "cn");

                    // trustPartner = DNS имя доверенного домена
                    var dnsName = GetAttributeValue(entry, "trustPartner");

                    // Если нет trustPartner, используем cn как DNS имя
                    if (string.IsNullOrEmpty(dnsName))
                    {
                        dnsName = netBiosName;
                    }

                    // Преобразуем DNS имя в DN
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

    /// <summary>
    /// Получает UPN-суффиксы для домена
    /// </summary>
    private List<string> GetUpnSuffixes(ILdapConnection connection, string domainDn, bool alternativeSuffixesEnabled)
    {
        var suffixes = new List<string>();
        // Основной суффикс всегда добавляем
        suffixes.Add(ExtractDnsFromDn(domainDn));

        // Если альтернативные отключены - только основной
        if (!alternativeSuffixesEnabled)
            return suffixes;

        try
        {
            // Ищем в контейнере Partitions в Configuration
            var configDn = $"CN=Partitions,CN=Configuration,{domainDn}";

            var searchRequest = new SearchRequest(
                configDn,
                "(objectClass=*)",
                SearchScope.Base,
                "uPNSuffixes");

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

    /// <summary>
    /// Добавляет домен в метаданные
    /// </summary>
    private void AddDomainToMetadata(ForestMetadata metadata, DomainInfo domainInfo)
    {
        // Добавляем в словарь доменов (по DNS имени)
        metadata.Domains[domainInfo.DnsName.ToLowerInvariant()] = domainInfo;

        // Добавляем в словарь NetBIOS имен
        if (!string.IsNullOrEmpty(domainInfo.NetBiosName))
        {
            metadata.NetBiosNames[domainInfo.NetBiosName.ToUpperInvariant()] = domainInfo;
        }

        // UPN-суффиксы добавим позже, после загрузки
    }

    /// <summary>
    /// Получает значение атрибута из записи
    /// </summary>
    private string? GetAttributeValue(SearchResultEntry entry, string attributeName)
    {
        if (entry.Attributes.Contains(attributeName))
        {
            var values = entry.Attributes[attributeName].GetValues(typeof(string));
            if (values != null && values.Length > 0)
            {
                return values[0]?.ToString();
            }
        }
        return null;
    }
}


