namespace Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline;

public class PipelineConfiguration
{
    public Type[] PipelineStepsTypes { get; }

    public PipelineConfiguration(Type[] pipelineStepsTypes)
    {
        if (pipelineStepsTypes is null)
            throw new ArgumentNullException(nameof(pipelineStepsTypes));
        PipelineStepsTypes = pipelineStepsTypes;
    }
}