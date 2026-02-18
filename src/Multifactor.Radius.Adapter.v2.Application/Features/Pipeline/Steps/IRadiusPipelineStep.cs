using Multifactor.Radius.Adapter.v2.Application.Core;

namespace Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.Steps;

public interface IRadiusPipelineStep
{
    Task ExecuteAsync(RadiusPipelineContext context);
}