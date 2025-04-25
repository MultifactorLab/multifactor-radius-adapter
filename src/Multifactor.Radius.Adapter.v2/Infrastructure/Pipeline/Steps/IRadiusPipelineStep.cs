using Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline.Context;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline.Steps;

public interface IRadiusPipelineStep
{
    public Task ExecuteAsync(IRadiusPipelineExecutionContext context);
}