using Multifactor.Radius.Adapter.v2.Application.Core;

namespace Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.Interfaces;

public interface IRadiusPipeline
{
    Task ExecuteAsync(RadiusPipelineContext context);
}