using Microsoft.Extensions.Logging;
using Multifactor.Radius.Adapter.v2.Core.AccessChallenge;
using Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline.Context;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline.Steps;

public class AccessChallengeStep : IRadiusPipelineStep
{
    private readonly IChallengeProcessorProvider _challengeProcessorProvider;
    private readonly ILogger<AccessChallengeStep> _logger;
    public AccessChallengeStep(IChallengeProcessorProvider challengeProcessorProvider, ILogger<AccessChallengeStep> logger)
    {
        _challengeProcessorProvider = challengeProcessorProvider;
        _logger = logger;
    }

    public async Task ExecuteAsync(IRadiusPipelineExecutionContext context)
    {
        _logger.LogDebug("'{name}' started", nameof(AccessChallengeStep));
        if (string.IsNullOrWhiteSpace(context.RequestPacket.State))
            return;

        var identifier = new ChallengeIdentifier(context.ClientConfigurationName, context.RequestPacket.State);
        var processor = _challengeProcessorProvider.GetChallengeProcessorByIdentifier(identifier);
        
        if (processor is null)
            return;
        
        var result = await processor.ProcessChallengeAsync(identifier, context);

        switch (result)
        {
            case ChallengeStatus.Accept:
                return;
            
            case ChallengeStatus.Reject:
            case ChallengeStatus.InProcess:
                context.ExecutionState.Terminate();
                return;
            
            default:
                throw new InvalidOperationException($"Unexpected challenge result: {result}");
        }
    }
}