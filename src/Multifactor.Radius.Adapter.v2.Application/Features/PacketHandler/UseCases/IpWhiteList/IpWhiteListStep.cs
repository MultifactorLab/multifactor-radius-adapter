using System.Net;
using Microsoft.Extensions.Logging;
using Multifactor.Radius.Adapter.v2.Application.Core.Enum;
using Multifactor.Radius.Adapter.v2.Application.Core.Models;

namespace Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.IpWhiteList;

internal sealed class IpWhiteListStep : IRadiusPipelineStep
{
    private readonly ILogger<IpWhiteListStep> _logger;
    private const string StepName = nameof(IpWhiteListStep);
    
    public IpWhiteListStep(ILogger<IpWhiteListStep> logger)
    {
        _logger = logger;
    }

    public Task ExecuteAsync(RadiusPipelineContext context)
    {
        _logger.LogDebug("'{name}' started", StepName);
        var ipWhiteList = context.ClientConfiguration.IpWhiteList;
        if (ipWhiteList.Count == 0) return Task.CompletedTask;
        
        var callingStationId = context.ClientConfiguration.IsIpFromUdp ? 
            context.RequestPacket.CallingStationIdAttribute :
            context.ClientConfiguration.CallingStationIdAttribute;

        var clientIp =  IPAddress.TryParse(callingStationId, out var callingStationIp)
            ? callingStationIp : context.RequestPacket.RemoteEndpoint?.Address;
        
        var isIpInRange = ipWhiteList.Any(x => x.Contains(clientIp));
        var rangesStr = string.Join(", ", ipWhiteList);
        if (isIpInRange)
        {
            _logger.LogDebug("Client '{clientIp}' is in the allowed IP range: ({ranges})", clientIp?.ToString(), rangesStr);
            return Task.CompletedTask;
        }
        
        _logger.LogInformation("Client '{clientIp}' is not in the allowed IP range: ({ranges})", clientIp?.ToString(), rangesStr);

        context.FirstFactorStatus = AuthenticationStatus.Reject;
        context.SecondFactorStatus = AuthenticationStatus.Reject;
        context.Terminate();
        return Task.CompletedTask;
    }
}