using Microsoft.Extensions.Logging;
using Multifactor.Radius.Adapter.v2.Application.Cache;
using Multifactor.Radius.Adapter.v2.Application.Core;
using Multifactor.Radius.Adapter.v2.Application.Features.Ldap.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.Ldap.Ports;
using Multifactor.Radius.Adapter.v2.Application.Features.LoadLdapForest.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.LoadLdapForest.Port;
using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.Models;
using System.DirectoryServices.Protocols;

namespace Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.Steps;

/// <summary>
/// Шаг загрузки метаданных леса Active Directory
/// </summary>
public class LoadLdapForestStep : IRadiusPipelineStep
{
    private readonly ILdapForestLoad _ldapForestLoad;
    private readonly ICacheService _cache;
    private readonly ILogger<LoadLdapForestStep> _logger;

    private const string CacheKeyPrefix = "ForestMetadata";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(1);

    public LoadLdapForestStep(
        ILdapForestLoad ldapForestLoad,
        ICacheService cache,
        ILogger<LoadLdapForestStep> logger)
    {
        _ldapForestLoad = ldapForestLoad;
        _cache = cache;
        _logger = logger;
    }

    public Task ExecuteAsync(RadiusPipelineContext context)
    {
        _logger.LogDebug("'{name}' started", nameof(LoadLdapForestStep));

        if (!context.IsDomainAccount)
        {
            _logger.LogDebug("Not a domain account, skipping");
            return Task.CompletedTask;
        }

        ArgumentNullException.ThrowIfNull(context.LdapConfiguration);

        var forestMetadata = TryGetForestMetadata(context);

        if (forestMetadata is null)
        {
            _logger.LogWarning("Unable to load forest metadata, using fallback");
            forestMetadata = CreateFallbackMetadata(context.LdapConfiguration.ConnectionString);
        }

        // Применяем фильтры доменов из конфига
        if (context.LdapConfiguration.IncludedDomains.Count > 0 ||
            context.LdapConfiguration.ExcludedDomains.Count > 0)
        {
            forestMetadata = ApplyDomainFilters(forestMetadata,
                context.LdapConfiguration.IncludedDomains,
                context.LdapConfiguration.ExcludedDomains);
        }

        context.ForestMetadata = forestMetadata;

        _logger.LogInformation(
            "Loaded forest metadata: {domainCount} domains, {suffixCount} suffixes (trusted:{trusted}, altSuffixes:{altSuffixes})",
            forestMetadata.Domains.Count,
            forestMetadata.UpnSuffixes.Count,
            context.LdapConfiguration.TrustedDomainsEnabled,
            context.LdapConfiguration.AlternativeSuffixesEnabled);

        return Task.CompletedTask;
    }

    private IForestMetadata? TryGetForestMetadata(RadiusPipelineContext context)
    {
        var cacheKey = $"{CacheKeyPrefix}:{context.LdapConfiguration!.ConnectionString}";

        if (_cache.TryGetValue(cacheKey, out IForestMetadata? cached))
            return cached;

        // Если доверенные домены отключены - только текущий домен
        if (!context.LdapConfiguration.TrustedDomainsEnabled)
        {
            _logger.LogDebug("Trusted domains disabled");
            var singleDomain = CreateSingleDomainMetadata(context);
            _cache.Set(cacheKey, singleDomain, DateTimeOffset.Now.Add(CacheDuration));
            return singleDomain;
        }

        var request = new LoadMetadataDto {
            ConnectionData = new LdapConnectionData
            {
                ConnectionString = context.LdapConfiguration.ConnectionString,
                UserName = context.LdapConfiguration.Username,
                Password = context.LdapConfiguration.Password,
                BindTimeoutInSeconds = context.LdapConfiguration.BindTimeoutSeconds,
            },
            AlternativeSuffixesEnabled = context.LdapConfiguration.AlternativeSuffixesEnabled
        };

        var metadata = _ldapForestLoad.Execute(request);

        if (metadata is not null)
        {
            _cache.Set(cacheKey, metadata, DateTimeOffset.Now.Add(CacheDuration));
        }

        return metadata;
    }

    private IForestMetadata CreateSingleDomainMetadata(RadiusPipelineContext context)
    {
        // Используем информацию из уже загруженной схемы!
        var namingContext = context.LdapSchema?.NamingContext;

        if (namingContext == null)
        {
            _logger.LogError("No schema available, falling back to connection string parsing");
            throw new Exception("No schema available, falling back to connection string parsing");
        }

        // Извлекаем DNS имя из DN (DC=company,DC=com → company.com)
        var dnsName = ExtractDnsFromDn(namingContext.StringRepresentation);
        var netBiosName = dnsName.Split('.')[0].ToUpperInvariant();

        _logger.LogDebug("Creating single domain metadata from schema: {dnsName} (DN: {dn})",
            dnsName, namingContext.StringRepresentation);

        var domainInfo = new DomainInfo
        {
            DnsName = dnsName,
            DistinguishedName = namingContext.StringRepresentation,
            NetBiosName = netBiosName,
            IsTrusted = false,
            UpnSuffixes = new List<string> { dnsName }
        };

        return new ForestMetadata
        {
            RootDomain = dnsName,
            Domains = new Dictionary<string, DomainInfo>
            {
                [dnsName.ToLowerInvariant()] = domainInfo
            },
            UpnSuffixes = new Dictionary<string, DomainInfo>
            {
                [dnsName.ToLowerInvariant()] = domainInfo
            },
            NetBiosNames = new Dictionary<string, DomainInfo>
            {
                [netBiosName] = domainInfo
            }
        };
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

    private IForestMetadata CreateFallbackMetadata(string connectionString)
    {
        // Fallback на случай, если не удалось загрузить лес
        // Используем только тот домен, который указан в connection string
        var uri = new Uri(connectionString);
        var domain = uri.Host;

        return new ForestMetadata
        {
            RootDomain = domain,
            Domains = new Dictionary<string, DomainInfo>
            {
                [domain] = new DomainInfo
                {
                    DnsName = domain,
                    DistinguishedName = ConvertToDn(domain),
                    NetBiosName = domain.Split('.')[0].ToUpperInvariant()
                }
            },
            UpnSuffixes = new Dictionary<string, DomainInfo>
            {
                [domain] = new DomainInfo
                {
                    DnsName = domain,
                    DistinguishedName = ConvertToDn(domain)
                }
            },
            NetBiosNames = new Dictionary<string, DomainInfo>
            {
                [domain.Split('.')[0].ToUpperInvariant()] = new DomainInfo
                {
                    DnsName = domain,
                    DistinguishedName = ConvertToDn(domain)
                }
            }
        };
    }

    private static string ConvertToDn(string domain)
    {
        var parts = domain.Split('.');
        return string.Join(",", parts.Select(p => $"DC={p}"));
    }

    private IForestMetadata ApplyDomainFilters(IForestMetadata metadata, IReadOnlyList<string> included, IReadOnlyList<string> excluded)
    {
        if (included.Count == 0 && excluded.Count == 0)
            return metadata;

        var filteredMetadata = new ForestMetadata
        {
            RootDomain = metadata.RootDomain
        };

        foreach (var domain in metadata.Domains.Values)
        {
            // Проверяем включение
            if (included.Count > 0 && !included.Any(i =>
                domain.DnsName.Equals(i, StringComparison.OrdinalIgnoreCase) ||
                domain.NetBiosName.Equals(i, StringComparison.OrdinalIgnoreCase)))
            {
                _logger.LogDebug("Domain {domain} excluded (not in included list)", domain.DnsName);
                continue;
            }

            // Проверяем исключение
            if (excluded.Count > 0 && excluded.Any(e =>
                domain.DnsName.Equals(e, StringComparison.OrdinalIgnoreCase) ||
                domain.NetBiosName.Equals(e, StringComparison.OrdinalIgnoreCase)))
            {
                _logger.LogDebug("Domain {domain} excluded (in excluded list)", domain.DnsName);
                continue;
            }

            // Добавляем домен в отфильтрованные метаданные
            AddDomainToMetadata(filteredMetadata, domain);

            // Добавляем UPN-суффиксы
            foreach (var suffix in domain.UpnSuffixes)
            {
                filteredMetadata.UpnSuffixes[suffix] = domain;
            }
        }

        _logger.LogInformation("Domain filters applied: {includedCount} included, {excludedCount} excluded. Result: {domainCount} domains",
            included.Count, excluded.Count, filteredMetadata.Domains.Count);

        return filteredMetadata;
    }
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
}