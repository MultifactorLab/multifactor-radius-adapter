namespace Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline;

public interface IRadiusPipeline
{
    Task ExecuteAsync(RadiusPipelineExecutionContext context);
}