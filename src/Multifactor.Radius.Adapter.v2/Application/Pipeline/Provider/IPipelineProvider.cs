using Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline;

namespace Multifactor.Radius.Adapter.v2.Application.Pipeline.Provider;

public interface IPipelineProvider
{
    IRadiusPipeline? GetRadiusPipeline(string key);
}