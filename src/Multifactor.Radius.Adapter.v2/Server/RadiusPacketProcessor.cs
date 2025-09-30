using Microsoft.Extensions.Logging;
using Multifactor.Radius.Adapter.v2.Core;
using Multifactor.Radius.Adapter.v2.Core.Configuration.Client;
using Multifactor.Radius.Adapter.v2.Core.Ldap.Forest;
using Multifactor.Radius.Adapter.v2.Core.Pipeline;
using Multifactor.Radius.Adapter.v2.Core.Pipeline.Settings;
using Multifactor.Radius.Adapter.v2.Core.Radius.Packet;
using Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline;
using Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline.Context;
using Multifactor.Radius.Adapter.v2.Services.AdapterResponseSender;
using Multifactor.Radius.Adapter.v2.Services.Ldap.Forest;

namespace Multifactor.Radius.Adapter.v2.Server;

public class RadiusPacketProcessor : IRadiusPacketProcessor
{
    private readonly IPipelineProvider _pipelineProvider;
    private readonly IResponseSender _responseSender;
    private readonly ILdapServerConfigurationService _ldapServerConfigurationService;
    private readonly ILdapForestService _ldapForestService;
    private readonly ILogger<RadiusPacketProcessor> _logger;

    public RadiusPacketProcessor(
        IPipelineProvider pipelineProvider,
        IResponseSender responseSender,
        ILdapServerConfigurationService ldapServerConfigurationService,
        ILdapForestService ldapForestService,
        ILogger<RadiusPacketProcessor> logger)
    {
        _pipelineProvider = pipelineProvider;
        _responseSender = responseSender;
        _ldapServerConfigurationService = ldapServerConfigurationService;
        _ldapForestService = ldapForestService;
        _logger = logger;
    }
    
    public async Task ProcessPacketAsync(IRadiusPacket requestPacket, IClientConfiguration clientConfiguration)
    {
        _logger.LogDebug("Start processing '{type}' packet.", requestPacket.Code);
        if (clientConfiguration.LdapServers.Count <= 0 || requestPacket.Code != PacketCode.AccessRequest)
        {
            await ExecutePipeline(clientConfiguration, requestPacket);
            return;
        }
        
        foreach (var serverConfig in clientConfiguration.LdapServers)
        {
            var forest = _ldapForestService.LoadLdapForest(
                Utils.CreateLdapConnectionOptions(serverConfig),
                serverConfig.TrustedDomainsEnabled,
                serverConfig.AlternativeSuffixesEnabled);
            
            if (!forest.Any())
            {
                _logger.LogWarning("Failed to load LDAP forest for '{domain}'", serverConfig.ConnectionString);
                continue;
            }
            
            var filteredForest = ApplyPermissions(forest, serverConfig.DomainPermissions, serverConfig.SuffixesPermissions);
            
            var configs = GetLdapServerConfigurations(filteredForest, serverConfig);
            
            foreach (var config in configs)
            {
                var isSuccessful = await ExecutePipeline(clientConfiguration, requestPacket, config);
                if (isSuccessful)
                    return;
            }
        }
    }

    private async Task<bool> ExecutePipeline(IClientConfiguration clientConfiguration, IRadiusPacket requestPacket, ILdapServerConfiguration? ldapServerConfiguration = null)
    {
        var context = CreatePipelineContext(clientConfiguration, requestPacket, ldapServerConfiguration);
        var pipeline = GetPipeline(clientConfiguration.Name);
        var logMessage = $"Start executing pipeline for '{clientConfiguration.Name}'" + (ldapServerConfiguration is not null ? $" at '{ldapServerConfiguration.ConnectionString}'" : string.Empty);
        _logger.LogDebug(logMessage);
        
        try
        {
            await pipeline.ExecuteAsync(context);
            var responseRequest = GetResponseRequest(context);
            await _responseSender.SendResponse(responseRequest);
            return true;
        }
        catch (Exception e)
        {
            var errMessage = $"Failed to execute pipeline for '{clientConfiguration.Name}'" + (ldapServerConfiguration is not null ? $" at '{ldapServerConfiguration.ConnectionString}'" : string.Empty);
            _logger.LogWarning(exception: e, errMessage);
        }

        return false;
    }

    private RadiusPipelineExecutionContext CreatePipelineContext(IClientConfiguration clientConfiguration, IRadiusPacket requestPacket, ILdapServerConfiguration? ldapServerConfiguration = null)
    {
        var executionSetting = new PipelineExecutionSettings(clientConfiguration, ldapServerConfiguration);
        var context = new RadiusPipelineExecutionContext(executionSetting, requestPacket)
        {
            Passphrase = UserPassphrase.Parse(requestPacket.TryGetUserPassword(), clientConfiguration.PreAuthnMode)
        };
        return context;
    }

    private IRadiusPipeline GetPipeline(string clientConfigurationName)
    {
        var pipeline = _pipelineProvider.GetRadiusPipeline(clientConfigurationName);
        if (pipeline is null)
            throw new Exception($"No pipeline found for client {clientConfigurationName}, check adapter configuration and restart the adapter.");
        return pipeline;
    }
    
    private SendAdapterResponseRequest GetResponseRequest(IRadiusPipelineExecutionContext context) => new(context);

    private IEnumerable<LdapForestEntry> ApplyPermissions(IEnumerable<LdapForestEntry> forest, IPermissionRules domainPermissions, IPermissionRules suffixesPermissions)
    {
        var filter = new ForestFilter();
        var filtered = filter.FilterDomains(forest, domainPermissions);
        filtered = filter.FilterSuffixes(filtered, suffixesPermissions);
        return filtered;
    }

    private IEnumerable<ILdapServerConfiguration> GetLdapServerConfigurations(IEnumerable<LdapForestEntry> forest, ILdapServerConfiguration serverConfig)
    {
       return _ldapServerConfigurationService.DuplicateConfigurationForDn(forest.Select(x => x.Schema.NamingContext), serverConfig);
    }
}