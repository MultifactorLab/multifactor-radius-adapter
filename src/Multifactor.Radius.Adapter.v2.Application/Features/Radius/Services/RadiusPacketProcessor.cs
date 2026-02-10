using Microsoft.Extensions.Logging;
using Multifactor.Radius.Adapter.v2.Application.Configuration.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.Interfaces;
using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.Radius.Exceptions;
using Multifactor.Radius.Adapter.v2.Application.Features.Radius.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.Radius.Models.Enums;
using Multifactor.Radius.Adapter.v2.Application.Features.Radius.Ports;

namespace Multifactor.Radius.Adapter.v2.Application.Features.Radius.Services;

public class RadiusPacketProcessor : IRadiusPacketProcessor
{
    private readonly IPipelineProvider _pipelineProvider;
    private readonly IResponseSender _responseSender;
    private readonly ILogger<RadiusPacketProcessor> _logger;

    public RadiusPacketProcessor(
        IPipelineProvider pipelineProvider,
        IResponseSender responseSender,
        ILogger<RadiusPacketProcessor> logger)
    {
        _pipelineProvider = pipelineProvider ?? throw new ArgumentNullException(nameof(pipelineProvider));
        _responseSender = responseSender ?? throw new ArgumentNullException(nameof(responseSender));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    public async Task ProcessPacketAsync(RadiusPacket requestPacket, IClientConfiguration clientConfiguration)
    {
        ArgumentNullException.ThrowIfNull(requestPacket);
        ArgumentNullException.ThrowIfNull(clientConfiguration);

        _logger.LogDebug("Start processing '{PacketType}' packet for client '{ClientName}'.", 
            requestPacket.Code, clientConfiguration.Name);
        
        if (ShouldProcessWithoutLdap(requestPacket, clientConfiguration))
        {
            await ExecutePipeline(clientConfiguration, requestPacket);
            return;
        }
        
        await TryProcessWithLdapServers(clientConfiguration, requestPacket);
    }

    private async Task TryProcessWithLdapServers(IClientConfiguration clientConfiguration, RadiusPacket requestPacket)
    {
        var processedSuccessfully = false;
        Exception? lastException = null;
        
        foreach (var serverConfig in clientConfiguration.LdapServers)
        {
            try
            {
                var success = await ExecutePipeline(clientConfiguration, requestPacket, serverConfig);
                if (success)
                {
                    processedSuccessfully = true;
                    break;
                }
            }
            catch (Exception ex)
            {
                lastException = ex;
                _logger.LogWarning(ex, 
                    "Failed to process with LDAP server {ConnectionString} for client {ClientName}", 
                    serverConfig.ConnectionString, clientConfiguration.Name);
            }
        }
        
        if (!processedSuccessfully)
        {
            _logger.LogError(lastException, 
                "All LDAP servers failed for client {ClientName}", clientConfiguration.Name);
            throw new Exception(
                $"All LDAP servers failed for client '{clientConfiguration.Name}'", 
                lastException);
        }
    }

    private async Task<bool> ExecutePipeline(
        IClientConfiguration clientConfiguration, 
        RadiusPacket requestPacket, 
        ILdapServerConfiguration? ldapServerConfiguration = null)
    {
        if (ldapServerConfiguration != null)
        {
            _logger.LogDebug(
                "Executing pipeline for client {ClientName} with LDAP server {ConnectionString}", 
                clientConfiguration.Name, ldapServerConfiguration.ConnectionString);
        }
        else
        {
            _logger.LogDebug(
                "Executing pipeline for client {ClientName}", 
                clientConfiguration.Name);
        }
        
        var context = CreatePipelineContext(clientConfiguration, requestPacket, ldapServerConfiguration);
        var pipeline = GetPipeline(clientConfiguration);
        
        try
        {
            await pipeline.ExecuteAsync(context);
            
            var responseRequest = SendAdapterResponseRequest.FromContext(context);
            await _responseSender.SendResponse(responseRequest);
            
            return true;
        }
        catch (PipelineNotFoundException ex)
        {
            _logger.LogError(ex, "Pipeline configuration error for client {ClientName}", clientConfiguration.Name);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, 
                "Pipeline execution failed for client {ClientName}{ServerInfo}", 
                clientConfiguration.Name,
                ldapServerConfiguration != null ? $" with LDAP server {ldapServerConfiguration.ConnectionString}" : "");
            throw;
        }
    }

    private static RadiusPipelineContext CreatePipelineContext(
        IClientConfiguration clientConfiguration, 
        RadiusPacket requestPacket, 
        ILdapServerConfiguration? ldapServerConfiguration = null)
    {
        
        var password = requestPacket.TryGetUserPassword();
        var passphrase = UserPassphrase.Parse(password, clientConfiguration.PreAuthenticationMethod);
        
        var context = new RadiusPipelineContext(requestPacket, clientConfiguration, ldapServerConfiguration)
        {
            Passphrase = passphrase
        };
        
        return context;
    }

    private IRadiusPipeline GetPipeline(IClientConfiguration clientConfiguration)
    {
        var pipeline = _pipelineProvider.GetPipeline(clientConfiguration);
        if (pipeline is null)
        {
            throw new PipelineNotFoundException(
                $"No pipeline found for client '{clientConfiguration.Name}'. " +
                "Check adapter configuration and restart the adapter.",
                clientConfiguration.Name);
        }
        return pipeline;
    }
    
    private static bool ShouldProcessWithoutLdap(RadiusPacket requestPacket, IClientConfiguration clientConfiguration)
    {
        if (clientConfiguration.LdapServers.Count <= 0)
            return true;
        
        if (requestPacket.Code != PacketCode.AccessRequest)
            return true;
        
        return false;
    }
}