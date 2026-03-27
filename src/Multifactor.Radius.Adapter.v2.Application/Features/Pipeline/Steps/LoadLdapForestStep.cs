using Microsoft.Extensions.Logging;
using Multifactor.Radius.Adapter.v2.Application.Core.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.LoadLdapForest.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.LoadLdapForest.Port;

namespace Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.Steps;

/// <summary>
/// Шаг загрузки метаданных леса Active Directory
/// </summary>
internal sealed class LoadLdapForestStep : IRadiusPipelineStep
{
    private readonly ILdapForestLoad _ldapForestLoad;
    private readonly IForestCacheService _cache;
    private readonly ILogger<LoadLdapForestStep> _logger;

    private const string CacheKeyPrefix = "ForestMetadata";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(1);

    public LoadLdapForestStep(
        ILdapForestLoad ldapForestLoad,
        IForestCacheService cache,
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
            context.LdapConfiguration.EnableTrustedDomains,
            context.LdapConfiguration.AlternativeSuffixesEnabled);

        return Task.CompletedTask;
    }

    private IForestMetadata? TryGetForestMetadata(RadiusPipelineContext context)
    {
        var cacheKey = $"{CacheKeyPrefix}:{context.LdapConfiguration!.ConnectionString}";

        if (_cache.TryGetValue(cacheKey, out IForestMetadata? cached))
            return cached;

        if (!context.LdapConfiguration.EnableTrustedDomains)
        {
            _logger.LogDebug("Trusted domains disabled");
            var singleDomain = CreateSingleDomainMetadata(context);
            _cache.Set(cacheKey, singleDomain, DateTimeOffset.Now.Add(CacheDuration));
            return singleDomain;
        }

        var request = new LoadMetadataDto(
            context.LdapConfiguration.ConnectionString,
            context.LdapConfiguration.Username,
            context.LdapConfiguration.Password,
            context.LdapConfiguration.BindTimeoutSeconds,
            context.LdapConfiguration.AlternativeSuffixesEnabled);

        var metadata = _ldapForestLoad.Execute(request);

        if (metadata is not null)
        {
            _cache.Set(cacheKey, metadata, DateTimeOffset.Now.Add(CacheDuration));
        }

        return metadata;
    }

    private IForestMetadata CreateSingleDomainMetadata(RadiusPipelineContext context)
    {
        var namingContext = context.LdapSchema?.NamingContext;

        if (namingContext is null)
        {
            _logger.LogError("No schema available, falling back to connection string parsing");
            throw new Exception("No schema available, falling back to connection string parsing");
        }

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
            UpnSuffixes = [dnsName]
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
    
    private static string ExtractDnsFromDn(string dn)
    {
        var parts = dn.Split(',')
            .Where(p => p.Trim().StartsWith("DC=", StringComparison.OrdinalIgnoreCase))
            .Select(p => p.Substring(3).Trim())
            .ToArray();

        return string.Join(".", parts);
    }

    private static IForestMetadata CreateFallbackMetadata(string connectionString)
    {
        var uri = new Uri(connectionString);
        var domain = uri.Host;

        return new ForestMetadata
        {
            RootDomain = domain,
            Domains = new Dictionary<string, DomainInfo>
            {
                [domain] = new()
                {
                    DnsName = domain,
                    DistinguishedName = ConvertToDn(domain),
                    NetBiosName = domain.Split('.')[0].ToUpperInvariant()
                }
            },
            UpnSuffixes = new Dictionary<string, DomainInfo>
            {
                [domain] = new()
                {
                    DnsName = domain,
                    DistinguishedName = ConvertToDn(domain)
                }
            },
            NetBiosNames = new Dictionary<string, DomainInfo>
            {
                [domain.Split('.')[0].ToUpperInvariant()] = new()
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
            if (included.Count > 0 && !included.Any(i =>
                domain.DnsName.Equals(i, StringComparison.OrdinalIgnoreCase) ||
                domain.NetBiosName.Equals(i, StringComparison.OrdinalIgnoreCase)))
            {
                _logger.LogDebug("Domain {domain} excluded (not in included list)", domain.DnsName);
                continue;
            }

            if (excluded.Count > 0 && excluded.Any(e =>
                domain.DnsName.Equals(e, StringComparison.OrdinalIgnoreCase) ||
                domain.NetBiosName.Equals(e, StringComparison.OrdinalIgnoreCase)))
            {
                _logger.LogDebug("Domain {domain} excluded (in excluded list)", domain.DnsName);
                continue;
            }

            AddDomainToMetadata(filteredMetadata, domain);

            foreach (var suffix in domain.UpnSuffixes)
            {
                filteredMetadata.UpnSuffixes[suffix] = domain;
            }
        }

        _logger.LogInformation("Domain filters applied: {includedCount} included, {excludedCount} excluded. Result: {domainCount} domains",
            included.Count, excluded.Count, filteredMetadata.Domains.Count);

        return filteredMetadata;
    }
    
    private static void AddDomainToMetadata(ForestMetadata metadata, DomainInfo domainInfo)
    {
        metadata.Domains[domainInfo.DnsName.ToLowerInvariant()] = domainInfo;
        
        if (!string.IsNullOrEmpty(domainInfo.NetBiosName))
        {
            metadata.NetBiosNames[domainInfo.NetBiosName.ToUpperInvariant()] = domainInfo;
        }
    }
}