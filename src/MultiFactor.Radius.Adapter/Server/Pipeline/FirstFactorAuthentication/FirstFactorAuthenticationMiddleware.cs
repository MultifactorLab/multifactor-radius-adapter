//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using MultiFactor.Radius.Adapter.Core.Framework.Context;
using MultiFactor.Radius.Adapter.Core.Framework.Pipeline;
using MultiFactor.Radius.Adapter.Core.Radius;
using MultiFactor.Radius.Adapter.Server.Pipeline.FirstFactorAuthentication.Processing;
using System;
using System.Threading.Tasks;

namespace MultiFactor.Radius.Adapter.Server.Pipeline.FirstFactorAuthentication
{
    public class FirstFactorAuthenticationMiddleware : IRadiusMiddleware
    {
        private readonly IFirstFactorAuthenticationProcessorProvider _firstAuthFactorProcessorProvider;

        public FirstFactorAuthenticationMiddleware(IFirstFactorAuthenticationProcessorProvider firstAuthFactorProcessorProvider)
        {
            _firstAuthFactorProcessorProvider = firstAuthFactorProcessorProvider ?? throw new ArgumentNullException(nameof(firstAuthFactorProcessorProvider));
        }

        public async Task InvokeAsync(RadiusContext context, RadiusRequestDelegate next)
        {
            if (context.Authentication.FirstFactor != AuthenticationCode.Awaiting)
            {
                await next(context);
                return;
            }

            var firstAuthProcessor = _firstAuthFactorProcessorProvider.GetProcessor(context.Configuration.FirstFactorAuthenticationSource);
            var firstFactorAuthenticationResultCode = await firstAuthProcessor.ProcessFirstAuthFactorAsync(context);
            if (firstFactorAuthenticationResultCode == PacketCode.AccessAccept)
            {
                context.Authentication.SetFirstFactor(AuthenticationCode.Accept);
                await next(context);
                return;
            }

            // first factor authentication rejected
            context.Authentication.SetFirstFactor(AuthenticationCode.Reject);

            // stop authencation process
            return;
        }
    }
}