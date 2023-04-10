//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using MultiFactor.Radius.Adapter.Core;
using MultiFactor.Radius.Adapter.Core.Pipeline;
using MultiFactor.Radius.Adapter.Core.Radius;
using System;
using System.Threading.Tasks;

namespace MultiFactor.Radius.Adapter.Server.Pipeline
{
    public class StatusServerMiddleware : IRadiusMiddleware
    {
        private readonly ServerInfo _serverInfo;
        private readonly RadiusRequestPostProcessor _requestPostProcessor;

        public StatusServerMiddleware(ServerInfo serverInfo, RadiusRequestPostProcessor requestPostProcessor)
        {
            _serverInfo = serverInfo ?? throw new ArgumentNullException(nameof(serverInfo));
            _requestPostProcessor = requestPostProcessor ?? throw new ArgumentNullException(nameof(requestPostProcessor));
        }

        public async Task InvokeAsync(RadiusContext context, RadiusRequestDelegate next)
        {
            if (context.RequestPacket.Code != PacketCode.StatusServer)
            {
                await next(context);
                return;
            }

            var uptime = _serverInfo.GetUptime();
            context.ReplyMessage = $"Server up {uptime.Days} days {uptime.ToString("hh\\:mm\\:ss")}";
            context.ResponseCode = PacketCode.AccessAccept;

            await _requestPostProcessor.InvokeAsync(context);
        }
    }
}