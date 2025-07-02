using Microsoft.Extensions.Logging;
using Multifactor.Radius.Adapter.v2.Core.AccessChallenge;
using Multifactor.Radius.Adapter.v2.Core.Auth;
using Multifactor.Radius.Adapter.v2.Core.FirstFactor;
using Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline.Context;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline.Steps;

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

    public async Task ExecuteAsync(IRadiusPipelineExecutionContext context)
    {
        _logger.LogDebug("'{name}' started", nameof(FirstFactorStep));
        ArgumentNullException.ThrowIfNull(context, nameof(context));

        if (context.AuthenticationState.FirstFactorStatus != AuthenticationStatus.Awaiting)
            return;

        var processor = _firstFactorProcessor.GetProcessor(context.FirstFactorAuthenticationSource);
        await processor.ProcessFirstFactor(context);
        
        if (!string.IsNullOrWhiteSpace(context.MustChangePasswordDomain))
        {
            var challengeProcessor = _challengeProcessorProviderProvider.GetChallengeProcessorByType(ChallengeType.PasswordChange);
            if (challengeProcessor is null)
                throw new Exception($"Challenge processor for {context.FirstFactorAuthenticationSource} is not available");
            
            challengeProcessor.AddChallengeContext(context);
            context.AuthenticationState.FirstFactorStatus = AuthenticationStatus.Awaiting;
        }

        if (context.AuthenticationState.FirstFactorStatus != AuthenticationStatus.Accept)
            context.ExecutionState.Terminate();
    }
}