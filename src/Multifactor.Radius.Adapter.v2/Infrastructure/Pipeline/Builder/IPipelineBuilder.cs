using Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline.Steps;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline.Builder;

public interface IPipelineBuilder
{
    public IPipelineBuilder AddPipelineStep(IRadiusPipelineStep step);
    IRadiusPipeline? Build();
}