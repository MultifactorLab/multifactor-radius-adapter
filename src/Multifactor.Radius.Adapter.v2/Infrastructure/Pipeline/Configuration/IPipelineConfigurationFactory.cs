namespace Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline.Configuration;

public interface IPipelineConfigurationFactory
{
    public PipelineConfiguration CreatePipelineConfiguration(IPipelineStepsConfiguration pipelineStepsConfiguration);
}