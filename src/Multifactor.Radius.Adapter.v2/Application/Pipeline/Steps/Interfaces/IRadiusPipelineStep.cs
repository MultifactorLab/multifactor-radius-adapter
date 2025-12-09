using Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline;

namespace Multifactor.Radius.Adapter.v2.Application.Pipeline.Steps.Interfaces;

public interface IRadiusPipelineStep
{
    Task ExecuteAsync(RadiusPipelineExecutionContext context);
}