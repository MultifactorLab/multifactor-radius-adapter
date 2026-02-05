using Multifactor.Radius.Adapter.v2.Application.Configuration.Models;

namespace Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.Interfaces;

public interface IPipelineProvider
{
    public IRadiusPipeline GetPipeline(IClientConfiguration clientConfiguration);
}