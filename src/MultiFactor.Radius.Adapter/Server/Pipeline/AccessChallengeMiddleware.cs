//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using MultiFactor.Radius.Adapter.Core.Pipeline;
using System;
using System.Threading.Tasks;

namespace MultiFactor.Radius.Adapter.Server.Pipeline
{
    public class AccessChallengeMiddleware : IRadiusMiddleware
    {
        private readonly ChallengeProcessor _challengeProcessor;
        private readonly RadiusRequestPostProcessor _requestPostProcessor;

        public AccessChallengeMiddleware(ChallengeProcessor challengeProcessor, RadiusRequestPostProcessor requestPostProcessor)
        {
            _challengeProcessor = challengeProcessor ?? throw new ArgumentNullException(nameof(challengeProcessor));
            _requestPostProcessor = requestPostProcessor ?? throw new ArgumentNullException(nameof(requestPostProcessor));
        }

        public async Task InvokeAsync(RadiusContext context, RadiusRequestDelegate next)
        {
            if (context.RequestPacket.Attributes.ContainsKey("State")) //Access-Challenge response 
            {
                var identifier = new ChallengeRequestIdentifier(context.ClientConfiguration, context.RequestPacket.GetString("State"));

                if (_challengeProcessor.HasState(identifier))
                {
                    // second request with Multifactor challenge
                    context.ResponseCode = await _challengeProcessor.ProcessChallenge(identifier, context);
                    context.State = identifier.RequestId;  //state for Access-Challenge message if otp is wrong (3 times allowed)

                    // stop authentication process after otp code verification
                    await _requestPostProcessor.InvokeAsync(context);
                    return;
                }
            }

            await next(context);
        }
    }
}