using Microsoft.Extensions.Logging;
using Multifactor.Radius.Adapter.v2.Application.Core.Enum;
using Multifactor.Radius.Adapter.v2.Application.Core.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.SharedServices;

namespace Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.AccessChallenge;

internal sealed class AccessChallengeStep : IRadiusPipelineStep
{
    private readonly IChallengeProcessorProvider _challengeProcessorProvider;
    private readonly ILogger<AccessChallengeStep> _logger;
    public AccessChallengeStep(IChallengeProcessorProvider challengeProcessorProvider, ILogger<AccessChallengeStep> logger)
    {
        _challengeProcessorProvider = challengeProcessorProvider;
        _logger = logger;
    }

    public async Task ExecuteAsync(RadiusPipelineContext context)
    {
        _logger.LogDebug("'{name}' started", nameof(AccessChallengeStep));
        if (string.IsNullOrWhiteSpace(context.RequestPacket.State))
        {
            _logger.LogDebug("Packet state is empty. Skipping step");
            return;
        }
        var identifier = new ChallengeIdentifier(context.ClientConfiguration.Name, context.RequestPacket.State);
        var processor = _challengeProcessorProvider.GetChallengeProcessorByIdentifier(identifier);
        
        if (processor is null)
        {
            _logger.LogDebug("Processor for {state} not found. Skipping step", context.RequestPacket.State);
            return;
        }
        
        var result = await processor.ProcessChallengeAsync(identifier, context);

        switch (result)
        {
            case ChallengeStatus.Accept:
                return;
            case ChallengeStatus.Reject:
            case ChallengeStatus.InProcess:
                context.Terminate();
                return;
            default:
                throw new InvalidOperationException($"Unexpected challenge result: {result}");
        }
    }
}