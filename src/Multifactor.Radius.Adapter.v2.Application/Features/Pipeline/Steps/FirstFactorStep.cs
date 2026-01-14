using Microsoft.Extensions.Logging;
using Multifactor.Radius.Adapter.v2.Application.Features.AccessChallenge;
using Multifactor.Radius.Adapter.v2.Application.Features.AccessChallenge.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.FirstFactor;
using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.Models;
using Multifactor.Radius.Adapter.v2.Application.Models.Enum;

namespace Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.Steps;

public class FirstFactorStep : IRadiusPipelineStep
{
    private readonly IFirstFactorProcessorProvider _firstFactorProcessor;
    private readonly IChallengeProcessorProvider _challengeProcessorProviderProvider;
    private readonly ILogger<FirstFactorStep> _logger;

    public FirstFactorStep(IFirstFactorProcessorProvider processorProvider, IChallengeProcessorProvider challengeProcessorProviderProvider, ILogger<FirstFactorStep> logger)
    {
        _firstFactorProcessor = processorProvider;
        _challengeProcessorProviderProvider = challengeProcessorProviderProvider;
        _logger = logger;
    }

    public async Task ExecuteAsync(RadiusPipelineContext context)
    {
        _logger.LogDebug("'{name}' started", nameof(FirstFactorStep));
        ArgumentNullException.ThrowIfNull(context, nameof(context));

        if (context.FirstFactorStatus != AuthenticationStatus.Awaiting)
            return;

        var processor = _firstFactorProcessor.GetProcessor(context.ClientConfiguration.FirstFactorAuthenticationSource);
        await processor.ProcessFirstFactor(context);

        if (!string.IsNullOrWhiteSpace(context.MustChangePasswordDomain))
        {
            var challengeProcessor = _challengeProcessorProviderProvider.GetChallengeProcessorByType(ChallengeType.PasswordChange);
            if (challengeProcessor is null)
                throw new Exception($"Challenge processor for {context.ClientConfiguration.FirstFactorAuthenticationSource} is not available");

            challengeProcessor.AddChallengeContext(context);
            context.FirstFactorStatus = AuthenticationStatus.Awaiting;
        }
        
        if (context.FirstFactorStatus != AuthenticationStatus.Accept)
            context.Terminate();
    }
}