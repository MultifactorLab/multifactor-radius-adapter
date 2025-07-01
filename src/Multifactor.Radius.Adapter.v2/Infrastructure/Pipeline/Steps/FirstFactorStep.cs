using Microsoft.Extensions.Logging;
using Multifactor.Radius.Adapter.v2.Core.Auth;
using Multifactor.Radius.Adapter.v2.Core.FirstFactor;
using Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline.Context;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline.Steps;

public class FirstFactorStep : IRadiusPipelineStep
{
    private readonly IFirstFactorProcessorProvider _firstFactorProcessor;
    private readonly ILogger<FirstFactorStep> _logger;

    public FirstFactorStep(IFirstFactorProcessorProvider processorProvider, ILogger<FirstFactorStep> logger)
    {
        _firstFactorProcessor = processorProvider;
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

        if (context.AuthenticationState.FirstFactorStatus != AuthenticationStatus.Accept)
            context.ExecutionState.Terminate();
    }
}