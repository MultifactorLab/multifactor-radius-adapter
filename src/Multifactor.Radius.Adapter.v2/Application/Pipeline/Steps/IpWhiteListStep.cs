using System.Net;
using Microsoft.Extensions.Logging;
using Multifactor.Radius.Adapter.v2.Application.Pipeline.Steps.Interfaces;
using Multifactor.Radius.Adapter.v2.Domain.Auth;
using Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline;

namespace Multifactor.Radius.Adapter.v2.Application.Pipeline.Steps;

public class IpWhiteListStep : IRadiusPipelineStep
{
    private readonly ILogger<IpWhiteListStep> _logger;
    
    public IpWhiteListStep(ILogger<IpWhiteListStep> logger)
    {
        _logger = logger;
    }

    public Task ExecuteAsync(RadiusPipelineExecutionContext context)
    {
        var ipWhiteList = context.IpWhiteList;
        if (ipWhiteList.Count == 0)
            return Task.CompletedTask;
        
        var callingStationId = context.RequestPacket.CallingStationIdAttribute ?? string.Empty;

        var clientIp =  IPAddress.TryParse(callingStationId, out var callingStationIp)
            ? callingStationIp
            : context.RemoteEndpoint.Address;
        
        var isIpInRange = ipWhiteList.Any(x => x.Contains(clientIp));
        var rangesStr = string.Join(", ", ipWhiteList);
        if (isIpInRange)
        {
            _logger.LogDebug("Client '{clientIp}' is in the allowed IP range: ({ranges})", clientIp.ToString(), rangesStr);
            return Task.CompletedTask;
        }
        
        _logger.LogDebug("Client '{clientIp}' is not in the allowed IP range: ({ranges})", clientIp.ToString(), rangesStr);

        context.AuthenticationState.FirstFactorStatus = AuthenticationStatus.Reject;
        context.AuthenticationState.SecondFactorStatus = AuthenticationStatus.Reject;
        context.ExecutionState.Terminate();
        return Task.CompletedTask;
    }
}