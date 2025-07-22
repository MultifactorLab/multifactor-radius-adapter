using System.Linq;
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
        
        var clientIp = context.RemoteEndpoint.Address;
        var isIpInRange = ipWhiteList.Any(x => x.Contains(clientIp));

        if (isIpInRange)
        {
            await next(context);
            return;
        }
        
        var rangesStr = string.Join(", ", ipWhiteList);
        _logger.LogDebug("Client '{clientIp}' is not in the allowed IP range: ({ranges})", clientIp.ToString(), rangesStr);
        
        context.SetFirstFactorAuth(AuthenticationCode.Reject);
        context.SetSecondFactorAuth(AuthenticationCode.Reject);
        context.Flags.Terminate();
    }
}