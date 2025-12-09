using Multifactor.Radius.Adapter.v2.Application.Pipeline.Steps;
using Multifactor.Radius.Adapter.v2.Application.Pipeline.Steps.Interfaces;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline;

public class RadiusPipeline : IRadiusPipeline
{
    private readonly IRadiusPipelineStep? _currentStep;
    private readonly IRadiusPipeline? _nextStep;

    public RadiusPipeline(IRadiusPipelineStep? currentStep = null, IRadiusPipeline? nextStep = null)
    {
        _currentStep = currentStep;
        _nextStep = nextStep;
    }

    public async Task ExecuteAsync(RadiusPipelineExecutionContext context)
    {
        if (_currentStep is not null)
            await _currentStep.ExecuteAsync(context);
        
        if (context.ExecutionState.IsTerminated)
            return;
        
        if (_nextStep is not null)
            await _nextStep.ExecuteAsync(context);
    }
}