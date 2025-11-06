using Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline.Steps;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline;

public interface IPipelineConfigurationFactory
{
    public PipelineConfiguration CreatePipelineConfiguration(IPipelineStepsConfiguration pipelineStepsConfiguration);
}