using Multifactor.Radius.Adapter.v2.Application.Pipeline.Steps.Interfaces;
using Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline;

namespace Multifactor.Radius.Adapter.v2.Application.Pipeline.Builder;

public interface IPipelineBuilder
{
    public IPipelineBuilder AddPipelineStep(IRadiusPipelineStep step);
    IRadiusPipeline? Build();
}