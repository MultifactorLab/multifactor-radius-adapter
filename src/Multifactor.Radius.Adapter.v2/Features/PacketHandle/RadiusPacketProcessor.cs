using Microsoft.Extensions.Logging;
using Multifactor.Radius.Adapter.v2.Application.Core.Enum;
using Multifactor.Radius.Adapter.v2.Application.Core.Models;
using Multifactor.Radius.Adapter.v2.Application.Core.Models.Abstractions;
using Multifactor.Radius.Adapter.v2.Application.Core.Models.Packet;
using Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.Exceptions;
using Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.Pipeline;
using Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.Ports;

namespace Multifactor.Radius.Adapter.v2.Features.PacketHandle;

internal interface IRadiusPacketProcessor
{
    Task Execute(RadiusPacket requestPacket, IClientConfiguration clientConfiguration);
}

internal sealed class RadiusPacketProcessor : IRadiusPacketProcessor
{
    private readonly IResponseSender _responseSender;
    private readonly IPipelineProvider _pipelineProvider;
    private readonly ILogger<RadiusPacketProcessor> _logger;

    public RadiusPacketProcessor(
        IPipelineProvider pipelineProvider,
        IResponseSender responseSender,
        ILogger<RadiusPacketProcessor> logger)
    {
        _pipelineProvider = pipelineProvider;
        _responseSender = responseSender;
        _logger = logger;
    }
    
    public async Task Execute(RadiusPacket requestPacket, IClientConfiguration clientConfiguration)
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

        if (clientConfiguration.LdapServers != null)
            foreach (var serverConfig in clientConfiguration.LdapServers)
            {
                try
                {
                    var success = await ExecutePipeline(clientConfiguration, requestPacket, serverConfig);
                    if (!success) continue;
                    processedSuccessfully = true;
                    break;
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

    private IRadiusPipeline GetPipeline(IClientConfiguration clientConfiguration)
    {
        var pipeline = _pipelineProvider.GetPipeline(clientConfiguration);
        if (pipeline is null)
        {
            throw new PipelineNotFoundException($"No pipeline found for client '{clientConfiguration.Name}'. " +
                                                "Check adapter configuration and restart the adapter.");
        }
        return pipeline;
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
    
    private static bool ShouldProcessWithoutLdap(RadiusPacket requestPacket, IClientConfiguration clientConfiguration)
    {
        if (clientConfiguration.LdapServers?.Count <= 0)
            return true;
        return requestPacket.Code != PacketCode.AccessRequest;
    }
}