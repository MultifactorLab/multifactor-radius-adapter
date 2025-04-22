using Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline.Context;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline;

public interface IRadiusPipelineExecutor
{
    Task ExecuteAsync(IRadiusPipelineExecutionContext context);
}