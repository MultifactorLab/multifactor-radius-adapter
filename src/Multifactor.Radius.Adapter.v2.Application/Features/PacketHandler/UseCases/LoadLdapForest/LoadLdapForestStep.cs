using Microsoft.Extensions.Logging;
using Multifactor.Radius.Adapter.v2.Application.Core.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.LoadLdapForest.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.LoadLdapForest.Port;

namespace Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.LoadLdapForest;

/// <summary>
/// Шаг загрузки метаданных леса Active Directory
/// </summary>
internal sealed class LoadLdapForestStep : IRadiusPipelineStep
{
    private readonly ILoadLdapForest _ldapForestLoad;
    private readonly IForestCache _cache;
    private readonly ILogger<LoadLdapForestStep> _logger;
    private const string StepName = nameof(LoadLdapForestStep);

    public LoadLdapForestStep(
        ILoadLdapForest ldapForestLoad,
        IForestCache cache,
        ILogger<LoadLdapForestStep> logger)
    {
        _ldapForestLoad = ldapForestLoad;
        _cache = cache;
        _logger = logger;
    }

    public Task ExecuteAsync(RadiusPipelineContext context)
    {
        _logger.LogDebug("'{name}' started", StepName);

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
            return Task.CompletedTask;
        }

        if (context.LdapConfiguration.IncludedDomains?.Count > 0 ||
            context.LdapConfiguration.ExcludedDomains?.Count > 0)
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
        var cacheKey = context.LdapConfiguration!.ConnectionString;

        if (_cache.TryGetValue(cacheKey, out var cached))
            return cached;

        if (!context.LdapConfiguration.EnableTrustedDomains)
        {
            _logger.LogDebug("Trusted domains disabled");
            return null;
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
            _cache.Set(cacheKey, metadata);
        }

        return metadata;
    }

    private IForestMetadata ApplyDomainFilters(IForestMetadata metadata, IReadOnlyList<string>? included, IReadOnlyList<string>? excluded)
    {
        if ((included == null || included.Count == 0) && (excluded == null || excluded.Count == 0))
            return metadata;
        
        if(included?.Count > 0 && excluded?.Count > 0)
            throw new ArgumentException("Both included and excluded are set");

        var filteredMetadata = new ForestMetadata();

        foreach (var domain in metadata.Domains.Values)
        {
            if (included?.Count > 0 && !included.Any(i =>
                domain.DnsName.Equals(i, StringComparison.OrdinalIgnoreCase) ||
                domain.NetBiosName.Equals(i, StringComparison.OrdinalIgnoreCase)))
            {
                _logger.LogDebug("Domain {domain} excluded (not in included list)", domain.DnsName);
                continue;
            }

            if (excluded?.Count > 0 && excluded.Any(e =>
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

        _logger.LogDebug("Domain filters applied: {includedCount} included, {excludedCount} excluded. Result: {domainCount} domains",
            included?.Count, excluded?.Count, filteredMetadata.Domains.Count);

        return filteredMetadata;
    }
    
    private static void AddDomainToMetadata(ForestMetadata metadata, DomainInfo domainInfo)
    {
        metadata.Domains[domainInfo.DnsName.ToLowerInvariant()] = domainInfo;
        
        if (!string.IsNullOrEmpty(domainInfo.NetBiosName))
        {
            metadata.NetBiosNames[domainInfo.NetBiosName.ToLowerInvariant()] = domainInfo;
        }
    }
}