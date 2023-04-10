//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using MultiFactor.Radius.Adapter.Core.Pipeline;
using MultiFactor.Radius.Adapter.Core.Radius;
using Serilog;
using System;
using System.Threading.Tasks;

namespace MultiFactor.Radius.Adapter.Server.Pipeline
{
    public class Bypass2FaMidleware : IRadiusMiddleware
    {
        private readonly ILogger _logger;
        private readonly RadiusRequestPostProcessor _requestPostProcessor;

        public Bypass2FaMidleware(ILogger logger, RadiusRequestPostProcessor requestPostProcessor)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _requestPostProcessor = requestPostProcessor ?? throw new ArgumentNullException(nameof(requestPostProcessor));
        }

        public async Task InvokeAsync(RadiusContext context, RadiusRequestDelegate next)
        {
            if (!context.Bypass2Fa)
            {
                await next(context);
                return;
            }

            // second factor not required
            var userName = context.UserName;
            _logger.Information("Bypass second factor for user '{user:l}'", userName);

            context.ResponseCode = PacketCode.AccessAccept;
            // stop authencation process
            await _requestPostProcessor.InvokeAsync(context);
        }
    }
}