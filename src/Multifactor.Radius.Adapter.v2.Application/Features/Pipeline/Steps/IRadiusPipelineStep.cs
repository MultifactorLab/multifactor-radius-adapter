using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.Models;

namespace Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.Steps;

public interface IRadiusPipelineStep
{
    Task ExecuteAsync(RadiusPipelineContext context);
}