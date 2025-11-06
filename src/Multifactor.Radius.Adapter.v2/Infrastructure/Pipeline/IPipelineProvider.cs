namespace Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline;

public interface IPipelineProvider
{
    IRadiusPipeline? GetRadiusPipeline(string key);
}