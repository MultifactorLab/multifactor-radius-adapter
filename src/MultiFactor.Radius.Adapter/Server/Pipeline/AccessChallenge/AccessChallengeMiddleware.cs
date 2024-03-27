//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using MultiFactor.Radius.Adapter.Framework.Context;
using MultiFactor.Radius.Adapter.Framework.Pipeline;
using System;
using System.Threading.Tasks;

namespace MultiFactor.Radius.Adapter.Server.Pipeline.AccessChallenge
{
    public class AccessChallengeMiddleware : IRadiusMiddleware
    {
        private readonly ISecondFactorChallengeProcessor _challengeProcessor;
        private readonly IRadiusRequestPostProcessor _requestPostProcessor;

        public AccessChallengeMiddleware(ISecondFactorChallengeProcessor challengeProcessor, IRadiusRequestPostProcessor requestPostProcessor)
        {
            _challengeProcessor = challengeProcessor ?? throw new ArgumentNullException(nameof(challengeProcessor));
            _requestPostProcessor = requestPostProcessor ?? throw new ArgumentNullException(nameof(requestPostProcessor));
        }

        public async Task InvokeAsync(RadiusContext context, RadiusRequestDelegate next)
        {
            if (!context.RequestPacket.Attributes.ContainsKey("State"))
            {
                await next(context);
                return;
            }

            var identifier = new ChallengeRequestIdentifier(context.Configuration.Name, context.RequestPacket.GetString("State"));
            if (!_challengeProcessor.HasState(identifier))
            {
                await next(context);
                return;
            }

            // second request with Multifactor challenge
            var resultCode = await _challengeProcessor.ProcessChallengeAsync(identifier, context);
            switch (resultCode)
            {
                case ChallengeCode.Accept:
                    // 2fa was passed
                    context.Authentication.SetSecondFactor(AuthenticationCode.Accept);
                    await next(context); ;
                    break;

                case ChallengeCode.Reject:
                    context.Authentication.SetSecondFactor(AuthenticationCode.Reject);
                    context.State = identifier.RequestId;
                    return;

                case ChallengeCode.InProcess:
                    context.State = identifier.RequestId;
                    return;

                default:
                    throw new NotImplementedException($"Unexpected challenge rsult: {resultCode}");

            }
        }
    }
}