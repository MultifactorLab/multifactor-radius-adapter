//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using Microsoft.Extensions.Logging;
using MultiFactor.Radius.Adapter.Core.Pipeline;
using MultiFactor.Radius.Adapter.Core.Radius;
using MultiFactor.Radius.Adapter.Server.Context;
using System.Threading.Tasks;

namespace MultiFactor.Radius.Adapter.Server.Pipeline
{
    public class AccessRequestFilterMiddleware : IRadiusMiddleware
    {
        private readonly ILogger _logger;

        public AccessRequestFilterMiddleware(ILogger<AccessRequestFilterMiddleware> logger)
        {
            _logger = logger;
        }

        public async Task InvokeAsync(RadiusContext context, RadiusRequestDelegate next)
        {
            if (context.RequestPacket.Code == PacketCode.AccessRequest)
            {
                await next(context);
                return;
            }

            _logger.LogWarning("Unprocessable packet type: {code}", context.RequestPacket.Code);
        }
    }
}