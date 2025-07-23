using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MultiFactor.Radius.Adapter.Core.Framework.Context;
using MultiFactor.Radius.Adapter.Core.Framework.Pipeline;

namespace MultiFactor.Radius.Adapter.Server.Pipeline.IpWhiteList;

public class IpWhiteListMiddleware : IRadiusMiddleware
{
    private readonly ILogger<IpWhiteListMiddleware> _logger;
    
    public IpWhiteListMiddleware(ILogger<IpWhiteListMiddleware> logger)
    {
        _logger = logger;
    }

    public async Task InvokeAsync(RadiusContext context, RadiusRequestDelegate next)
    {
        var ipWhiteList = context.Configuration.IpWhiteAddressRanges;
        if (ipWhiteList.Count == 0)
        { 
            await next(context);
            return;
        }
        
        var callingStationId = context.RequestPacket.CallingStationId;
            
        var clientIp =  IPAddress.TryParse(callingStationId ?? string.Empty, out var callingStationIp)
            ? callingStationIp
            : context.RemoteEndpoint.Address;
        
        var isIpInRange = ipWhiteList.Any(x => x.Contains(clientIp));
        var rangesStr = string.Join(", ", ipWhiteList);
        
        if (isIpInRange)
        {
            _logger.LogDebug("Client '{clientIp}' is in the allowed IP range: ({ranges})", clientIp.ToString(), rangesStr);
            await next(context);
            return;
        }
        
        _logger.LogDebug("Client '{clientIp}' is not in the allowed IP range: ({ranges})", clientIp.ToString(), rangesStr);
        
        context.SetFirstFactorAuth(AuthenticationCode.Reject);
        context.SetSecondFactorAuth(AuthenticationCode.Reject);
        context.Flags.Terminate();
    }
}