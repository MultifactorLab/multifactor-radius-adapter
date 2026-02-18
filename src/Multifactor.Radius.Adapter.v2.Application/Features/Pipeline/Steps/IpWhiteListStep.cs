using System.Net;
using Microsoft.Extensions.Logging;
using Multifactor.Radius.Adapter.v2.Application.Core;
using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.Models.Enum;

namespace Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.Steps;

public class IpWhiteListStep : IRadiusPipelineStep
{
    private readonly ILogger<IpWhiteListStep> _logger;
    
    public IpWhiteListStep(ILogger<IpWhiteListStep> logger)
    {
        _logger = logger;
    }

    public Task ExecuteAsync(RadiusPipelineContext context)
    {
        var ipWhiteList = context.ClientConfiguration.IpWhiteList;
        if (ipWhiteList.Count == 0)
            return Task.CompletedTask;
        
        var callingStationId = context.RequestPacket.CallingStationIdAttribute ?? string.Empty;

        var clientIp =  IPAddress.TryParse(callingStationId, out var callingStationIp)
            ? callingStationIp
            : context.RequestPacket.RemoteEndpoint.Address;
        
        var isIpInRange = ipWhiteList.Any(x => x.Contains(clientIp));
        var rangesStr = string.Join(", ", ipWhiteList);
        if (isIpInRange)
        {
            _logger.LogDebug("Client '{clientIp}' is in the allowed IP range: ({ranges})", clientIp.ToString(), rangesStr);
            return Task.CompletedTask;
        }
        
        _logger.LogDebug("Client '{clientIp}' is not in the allowed IP range: ({ranges})", clientIp.ToString(), rangesStr);

        context.FirstFactorStatus = AuthenticationStatus.Reject;
        context.SecondFactorStatus = AuthenticationStatus.Reject;
        context.Terminate();
        return Task.CompletedTask;
    }
}