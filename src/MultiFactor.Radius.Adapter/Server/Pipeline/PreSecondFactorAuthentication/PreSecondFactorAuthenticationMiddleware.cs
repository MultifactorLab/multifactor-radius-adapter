using Microsoft.Extensions.Logging;
using MultiFactor.Radius.Adapter.Configuration;
using MultiFactor.Radius.Adapter.Configuration.Features.PreAuthModeFeature;
using MultiFactor.Radius.Adapter.Core.Radius;
using MultiFactor.Radius.Adapter.Framework.Context;
using MultiFactor.Radius.Adapter.Framework.Pipeline;
using MultiFactor.Radius.Adapter.Server.Pipeline.AccessChallenge;
using MultiFactor.Radius.Adapter.Services.MultiFactorApi;
using System;
using System.Threading.Tasks;

namespace MultiFactor.Radius.Adapter.Server.Pipeline.PreSecondFactorAuthentication
{
    public class PreSecondFactorAuthenticationMiddleware : IRadiusMiddleware
    {
        private readonly ISecondFactorChallengeProcessor _challengeProcessor;
        private readonly IMultifactorApiAdapter _apiAdapter;
        private readonly ILogger<PreSecondFactorAuthenticationMiddleware> _logger;

        public PreSecondFactorAuthenticationMiddleware(ISecondFactorChallengeProcessor challengeProcessor,
            IMultifactorApiAdapter apiAdapter,
            ILogger<PreSecondFactorAuthenticationMiddleware> logger)
        {
            _challengeProcessor = challengeProcessor;
            _apiAdapter = apiAdapter;
            _logger = logger;
        }

        public async Task InvokeAsync(RadiusContext context, RadiusRequestDelegate next)
        {
            var isBypassed = context.Authentication.SecondFactor == AuthenticationCode.Bypass;
            if (isBypassed)
            {
                _logger.LogInformation("Bypass pre-auth second factor for user '{user:l}' from {host:l}:{port}",
                    context.UserName, context.RemoteEndpoint.Address, context.RemoteEndpoint.Port);

                context.ResponseCode = context.Authentication.ToPacketCode();
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
                    context.ResponseCode = context.Authentication.ToPacketCode();
                    _logger.LogError("The pre-auth second factor was rejected: otp code is empty");
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
                        context.ResponseCode = context.Authentication.ToPacketCode();
                        return;
                    }

                    if (context.RequestPacket.IsVendorAclRequest)
                    {
                        // security check
                        if (context.FirstFactorAuthenticationSource == AuthenticationSource.Radius)
                        {
                            _logger.LogInformation("Bypass second factor for user '{user:l}' from {host:l}:{port}",
                                context.UserName,
                                context.RemoteEndpoint.Address,
                                context.RemoteEndpoint.Port);

                            context.SetSecondFactorAuth(AuthenticationCode.Bypass);
                            context.ResponseCode = context.Authentication.ToPacketCode();

                            await next(context);
                            return;
                        }
                    }

                    var response = await _apiAdapter.CreateSecondFactorRequestAsync(context);
                    context.State = response.State;
                    context.ReplyMessage = response.ReplyMessage;


                    if (response.Code == AuthenticationCode.Awaiting)
                    {
                        _challengeProcessor.AddState(context); 
                        context.ResponseCode = context.Authentication.ToPacketCode();
                        return;
                    }

                    if (response.Code != AuthenticationCode.Accept)
                    {
                        context.Authentication.SetSecondFactor(AuthenticationCode.Reject);
                        context.ResponseCode = context.Authentication.ToPacketCode();
                        _logger.LogError("The pre-auth second factor was rejected");
                        return;
                    }

                    context.SetSecondFactorAuth(AuthenticationCode.Accept);
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
