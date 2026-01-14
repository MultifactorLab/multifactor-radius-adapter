using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.Models;

namespace Multifactor.Radius.Adapter.v2.Application.Features.Pipeline;

public interface IRadiusPipeline
{
    Task ExecuteAsync(RadiusPipelineContext context);
}