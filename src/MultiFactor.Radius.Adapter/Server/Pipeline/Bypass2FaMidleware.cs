//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using Microsoft.Extensions.Logging;
using MultiFactor.Radius.Adapter.Core.Pipeline;
using MultiFactor.Radius.Adapter.Core.Radius;
using MultiFactor.Radius.Adapter.Server.Context;
using System;
using System.Threading.Tasks;

namespace MultiFactor.Radius.Adapter.Server.Pipeline
{
    public class Bypass2FaMidleware : IRadiusMiddleware
    {
        private readonly ILogger _logger;
        private readonly IRadiusRequestPostProcessor _requestPostProcessor;

        public Bypass2FaMidleware(ILogger<Bypass2FaMidleware> logger, IRadiusRequestPostProcessor requestPostProcessor)
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
            _logger.LogInformation("Bypass second factor for user '{user:l}'", context.UserName);

            context.ResponseCode = PacketCode.AccessAccept;
            context.AuthenticationState.SetSecondFactor(AuthenticationCode.Accept);

            // stop authencation process
        }
    }
}