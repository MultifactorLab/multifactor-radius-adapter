namespace Multifactor.Radius.Adapter.v2.Application.Features.Pipeline;

public interface IPipelineProvider
{
    IRadiusPipeline? GetPipeline(string key);
}