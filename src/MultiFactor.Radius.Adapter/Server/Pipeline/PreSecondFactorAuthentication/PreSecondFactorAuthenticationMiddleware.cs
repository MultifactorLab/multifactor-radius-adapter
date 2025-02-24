﻿using Microsoft.Extensions.Logging;
using MultiFactor.Radius.Adapter.Core.Framework.Context;
using MultiFactor.Radius.Adapter.Core.Framework.Pipeline;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration.Features.PreAuthModeFeature;
using MultiFactor.Radius.Adapter.Server.Pipeline.AccessChallenge;
using MultiFactor.Radius.Adapter.Services.MultiFactorApi;
using System;
using System.Threading.Tasks;

namespace MultiFactor.Radius.Adapter.Server.Pipeline.PreSecondFactorAuthentication
{
    public class PreSecondFactorAuthenticationMiddleware : IRadiusMiddleware
    {
        private readonly IChallengeProcessorProvider _challengeProcessorProvider;
        private readonly IMultifactorApiAdapter _apiAdapter;
        private readonly ILogger<PreSecondFactorAuthenticationMiddleware> _logger;

        public PreSecondFactorAuthenticationMiddleware(
            IChallengeProcessorProvider challengeProcessorProvider,
            IMultifactorApiAdapter apiAdapter,
            ILogger<PreSecondFactorAuthenticationMiddleware> logger)
        {
            _challengeProcessorProvider = challengeProcessorProvider;
            _apiAdapter = apiAdapter;
            _logger = logger;
        }

        public async Task InvokeAsync(RadiusContext context, RadiusRequestDelegate next)
        {
            var isBypassed = context.Authentication.SecondFactor == AuthenticationCode.Bypass;
            if (isBypassed)
            {
                _logger.LogInformation("Bypass pre-auth second factor for user '{user:l}' from {host:l}:{port}",
                    context.UserName, 
                    context.RemoteEndpoint.Address, 
                    context.RemoteEndpoint.Port);

                await next(context);
                return;
            }

            if (context.Authentication.SecondFactor != AuthenticationCode.Awaiting)
            {
                await next(context);
                return;
            }

            switch (context.PreAuthMode)
            {
                case PreAuthMode.Otp when context.Passphrase.Otp == null:
                    context.Authentication.SetSecondFactor(AuthenticationCode.Reject);
                    _logger.LogError("The pre-auth second factor was rejected: otp code is empty. User '{user:l}' from {host:l}:{port}",
                        context.UserName, 
                        context.RemoteEndpoint.Address, 
                        context.RemoteEndpoint.Port);
                    return;

                case PreAuthMode.Otp:
                case PreAuthMode.Push:
                case PreAuthMode.Telegram:

                    if (string.IsNullOrEmpty(context.SecondFactorIdentity))
                    {
                        _logger.LogWarning("Unable to process 2FA authentication for message id={id} from {host:l}:{port}: Can't find User-Name",
                            context.Header.Identifier,
                            context.RemoteEndpoint.Address,
                            context.RemoteEndpoint.Port);

                        context.SetSecondFactorAuth(AuthenticationCode.Reject);
                        return;
                    }

                    if (context.RequestPacket.IsVendorAclRequest)
                    {
                        // security check
                        if (context.FirstFactorAuthenticationSource == AuthenticationSource.Radius)
                        {
                            _logger.LogInformation("Bypass pre-auth second factor for user '{user:l}' from {host:l}:{port}",
                                context.UserName,
                                context.RemoteEndpoint.Address,
                                context.RemoteEndpoint.Port);

                            context.SetSecondFactorAuth(AuthenticationCode.Bypass);

                            await next(context);
                            return;
                        }
                    }

                    var response = await _apiAdapter.CreateSecondFactorRequestAsync(context);
                    context.SetMessageState(response.State);
                    context.SetReplyMessage(response.ReplyMessage);
                    context.SetSecondFactorAuth(response.Code);

                    if (response.Code == AuthenticationCode.Awaiting)
                    {
                        var challengeProcessor =
                            _challengeProcessorProvider.GetChallengeProcessorByType(ChallengeType.SecondFactor);
                        challengeProcessor.AddChallengeContext(context);
                        return;
                    }

                    if (response.Code == AuthenticationCode.Reject)
                    {
                        _logger.LogError("The pre-auth second factor was rejected for user '{user:l}' from {host:l}:{port}",
                            context.UserName,
                            context.RemoteEndpoint.Address,
                            context.RemoteEndpoint.Port);
                        return;
                    }

                    break;

                case PreAuthMode.None:
                    break;

                default:
                    throw new NotImplementedException($"Unknown pre-auth method: {context.PreAuthMode}");
            }

            await next(context);
        }
    }
}
