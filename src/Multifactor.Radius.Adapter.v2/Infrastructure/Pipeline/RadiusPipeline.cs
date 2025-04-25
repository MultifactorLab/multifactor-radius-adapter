using Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline.Context;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline;

public class RadiusPipeline : IRadiusPipeline
{
    private readonly Func<IRadiusPipelineExecutionContext, Task> _currentStep;
    private readonly Func<IRadiusPipelineExecutionContext, Task> _nextStep;
    
    public RadiusPipeline(Func<IRadiusPipelineExecutionContext, Task> currentStep, Func<IRadiusPipelineExecutionContext, Task> nextStep)
    {
        _currentStep = currentStep;
        _nextStep = nextStep;
    }

    public async Task ExecuteAsync(IRadiusPipelineExecutionContext context)
    {
        await _currentStep(context);
        await _nextStep(context);
    }
}