using Microsoft.Extensions.Logging;
using MultiFactor.Radius.Adapter.Configuration;
using MultiFactor.Radius.Adapter.Configuration.Features.PreAuthModeFeature;
using MultiFactor.Radius.Adapter.Core.Radius;
using MultiFactor.Radius.Adapter.Framework.Context;
using MultiFactor.Radius.Adapter.Framework.Pipeline;
using System;
using System.Threading.Tasks;

namespace MultiFactor.Radius.Adapter.Server.Pipeline.PreSecondFactorAuthentication
{
    public class PreSecondFactorAuthenticationMiddleware : IRadiusMiddleware
    {
        private readonly ILogger<PreSecondFactorAuthenticationMiddleware> _logger;

        public PreSecondFactorAuthenticationMiddleware(ILogger<PreSecondFactorAuthenticationMiddleware> logger)
        {
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
                    var respCode = await ProcessSecondAuthenticationFactor(request);
                    if (respCode == PacketCode.AccessChallenge)
                    {
                        AddStateChallengePendingRequest(request.State, request);
                        context.ResponseCode = context.Authentication.ToPacketCode();
                        CreateAndSendRadiusResponse(request);
                        return;
                    }

                    if (respCode != PacketCode.AccessAccept)
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

        /// <summary>
        /// Authenticate request at MultiFactor with user-name only
        /// </summary>
        private async Task<PacketCode> ProcessSecondAuthenticationFactor(RadiusContext context)
        {
            if (string.IsNullOrEmpty(context.SecondFactorIdentity))
            {
                _logger.LogWarning("Can't find User-Name in message id={id} from {host:l}:{port}", context.RequestPacket.Header.Identifier, context.RemoteEndpoint.Address, context.RemoteEndpoint.Port);
                return PacketCode.AccessReject;
            }

            if (context.RequestPacket.IsVendorAclRequest)
            {
                // security check
                if (context.Configuration.FirstFactorAuthenticationSource == AuthenticationSource.Radius)
                {
                    _logger.LogInformation("Bypass second factor for user {user:l}", context.UserName);
                    return PacketCode.AccessAccept;
                }
            }

            return await _multiFactorApiClient.CreateSecondFactorRequest(context);
        }
    }
}
