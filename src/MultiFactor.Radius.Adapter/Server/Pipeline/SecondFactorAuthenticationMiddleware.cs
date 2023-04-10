//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using MultiFactor.Radius.Adapter.Configuration;
using MultiFactor.Radius.Adapter.Core.Exceptions;
using MultiFactor.Radius.Adapter.Core.Pipeline;
using MultiFactor.Radius.Adapter.Core.Radius;
using MultiFactor.Radius.Adapter.Services.MultiFactorApi;
using Serilog;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace MultiFactor.Radius.Adapter.Server.Pipeline
{
    public class SecondFactorAuthenticationMiddleware : IRadiusMiddleware
    {
        private readonly ChallengeProcessor _challengeProcessor;
        private readonly MultiFactorApiClient _multiFactorApiClient;
        private readonly RadiusRequestPostProcessor _requestPostProcessor;
        private readonly ILogger _logger;

        public SecondFactorAuthenticationMiddleware(ChallengeProcessor challengeProcessor, MultiFactorApiClient multiFactorApiClient, 
            RadiusRequestPostProcessor requestPostProcessor, ILogger logger)
        {
            _challengeProcessor = challengeProcessor ?? throw new ArgumentNullException(nameof(challengeProcessor));
            _multiFactorApiClient = multiFactorApiClient ?? throw new ArgumentNullException(nameof(multiFactorApiClient));
            _requestPostProcessor = requestPostProcessor ?? throw new ArgumentNullException(nameof(requestPostProcessor));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task InvokeAsync(RadiusContext context, RadiusRequestDelegate next)
        {
            context.ResponseCode = await ProcessSecondAuthenticationFactor(context);
            if (context.ResponseCode == PacketCode.AccessChallenge)
            {
                _challengeProcessor.AddState(new ChallengeRequestIdentifier(context.ClientConfiguration, context.State), context);
            }

            await _requestPostProcessor.InvokeAsync(context);
            await next(context);
        }

        /// <summary>
        /// Authenticate request at MultiFactor with user-name only
        /// </summary>
        private async Task<PacketCode> ProcessSecondAuthenticationFactor(RadiusContext request)
        {
            var userName = request.UserName;

            if (string.IsNullOrEmpty(userName))
            {
                _logger.Warning("Can't find User-Name in message id={id} from {host:l}:{port}", request.RequestPacket.Identifier, request.RemoteEndpoint.Address, request.RemoteEndpoint.Port);
                return PacketCode.AccessReject;
            }

            if (request.RequestPacket.IsVendorAclRequest == true)
            {
                //security check
                if (request.ClientConfiguration.FirstFactorAuthenticationSource == AuthenticationSource.Radius)
                {
                    _logger.Information("Bypass second factor for user {user:l}", userName);
                    return PacketCode.AccessAccept;
                }
            }

            var response = await _multiFactorApiClient.CreateSecondFactorRequest(request);

            return response;
        }
    }
}