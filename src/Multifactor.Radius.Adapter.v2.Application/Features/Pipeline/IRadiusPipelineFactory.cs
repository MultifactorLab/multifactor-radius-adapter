using Multifactor.Radius.Adapter.v2.Application.Configuration.Models;

namespace Multifactor.Radius.Adapter.v2.Application.Features.Pipeline;

public interface IRadiusPipelineFactory
{
    IRadiusPipeline CreatePipeline(ClientConfiguration clientConfig);
}