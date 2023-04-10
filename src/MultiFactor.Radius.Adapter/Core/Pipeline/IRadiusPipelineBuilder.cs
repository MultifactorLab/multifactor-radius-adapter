namespace MultiFactor.Radius.Adapter.Core.Pipeline;

public interface IRadiusPipelineBuilder
{
    IRadiusPipelineBuilder Use<TMiddleware>() where TMiddleware : IRadiusMiddleware;
}
