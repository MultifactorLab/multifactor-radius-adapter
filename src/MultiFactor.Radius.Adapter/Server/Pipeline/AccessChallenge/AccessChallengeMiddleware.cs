//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using MultiFactor.Radius.Adapter.Core.Framework.Context;
using MultiFactor.Radius.Adapter.Core.Framework.Pipeline;
using System;
using System.Threading.Tasks;

namespace MultiFactor.Radius.Adapter.Server.Pipeline.AccessChallenge
{
    public class AccessChallengeMiddleware : IRadiusMiddleware
    {
        private readonly IChallengeProcessorProvider _challengeProcessorProvider;

        public AccessChallengeMiddleware(IChallengeProcessorProvider challengeProcessorProvider)
        {
            _challengeProcessorProvider = challengeProcessorProvider ?? throw new ArgumentNullException(nameof(challengeProcessorProvider));
        }

        public async Task InvokeAsync(RadiusContext context, RadiusRequestDelegate next)
        {
            if (!context.RequestPacket.Attributes.ContainsKey("State"))
            {
                await next(context);
                return;
            }

            var identifier = new ChallengeIdentifier(context.Configuration.Name, context.RequestPacket.GetString("State"));

            // second request with Multifactor challenge
            var challengeProcessor = _challengeProcessorProvider.GetChallengeProcessorForIdentifier(identifier);
            if (challengeProcessor == null)
            {
                await next(context);
                return;
            }

            var result = await challengeProcessor.ProcessChallengeAsync(identifier, context);
            
            switch (result)
            {
                case ChallengeCode.Accept:
                    await next(context);
                    break;

                case ChallengeCode.Reject:
                case ChallengeCode.InProcess:
                    context.Flags.Terminate();
                    return;

                default:
                    throw new NotImplementedException($"Unexpected challenge result: {result}");

            }
        }
    }
}