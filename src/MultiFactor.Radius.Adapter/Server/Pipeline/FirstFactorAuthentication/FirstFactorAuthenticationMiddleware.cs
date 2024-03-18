﻿//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using MultiFactor.Radius.Adapter.Core.Radius;
using MultiFactor.Radius.Adapter.Framework.Context;
using MultiFactor.Radius.Adapter.Framework.Pipeline;
using MultiFactor.Radius.Adapter.Server.Pipeline.FirstFactorAuthentication.FirstAuthFactorProcessing;
using System;
using System.Threading.Tasks;

namespace MultiFactor.Radius.Adapter.Server.Pipeline.FirstFactorAuthentication
{
    public class FirstFactorAuthenticationMiddleware : IRadiusMiddleware
    {
        private readonly IFirstAuthFactorProcessorProvider _firstAuthFactorProcessorProvider;
        private readonly IRadiusRequestPostProcessor _requestPostProcessor;

        public FirstFactorAuthenticationMiddleware(IFirstAuthFactorProcessorProvider firstAuthFactorProcessorProvider, IRadiusRequestPostProcessor requestPostProcessor)
        {
            _firstAuthFactorProcessorProvider = firstAuthFactorProcessorProvider ?? throw new ArgumentNullException(nameof(firstAuthFactorProcessorProvider));
            _requestPostProcessor = requestPostProcessor ?? throw new ArgumentNullException(nameof(requestPostProcessor));
        }

        public async Task InvokeAsync(RadiusContext context, RadiusRequestDelegate next)
        {
            var firstAuthProcessor = _firstAuthFactorProcessorProvider.GetProcessor(context.ClientConfiguration.FirstFactorAuthenticationSource);
            var firstFactorAuthenticationResultCode = await firstAuthProcessor.ProcessFirstAuthFactorAsync(context);
            if (firstFactorAuthenticationResultCode == PacketCode.AccessAccept)
            {
                context.AuthenticationState.SetFirstFactor(AuthenticationCode.Accept);
                await next(context);
                return;
            }

            // first factor authentication rejected
            context.ResponseCode = firstFactorAuthenticationResultCode;
            context.AuthenticationState.SetFirstFactor(AuthenticationCode.Reject);

            // stop authencation process
        }
    }
}