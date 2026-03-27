using Multifactor.Radius.Adapter.v2.Application.Core.Models;

namespace Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.Steps;

internal interface IRadiusPipelineStep
{
    Task ExecuteAsync(RadiusPipelineContext context);
}