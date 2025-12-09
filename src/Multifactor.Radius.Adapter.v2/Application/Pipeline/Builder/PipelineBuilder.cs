using Multifactor.Radius.Adapter.v2.Application.Pipeline.Steps.Interfaces;
using Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline;

namespace Multifactor.Radius.Adapter.v2.Application.Pipeline.Builder;

public class PipelineBuilder : IPipelineBuilder
{
    private readonly List<IRadiusPipelineStep> _pipelineSteps = [];

    public IPipelineBuilder AddPipelineStep(IRadiusPipelineStep step)
    {
        _pipelineSteps.Add(step);
        return this;
    }

    public IRadiusPipeline Build()
    {
        var nextStep = new RadiusPipeline();
        if (_pipelineSteps.Count == 0)
            return nextStep;

        RadiusPipeline? pipeline = null;
        for (int i = _pipelineSteps.Count - 1; i >= 0; i--)
        {
            pipeline = new RadiusPipeline(currentStep: _pipelineSteps[i], nextStep: nextStep);
            nextStep = pipeline;
        }

        return pipeline!;
    }
}