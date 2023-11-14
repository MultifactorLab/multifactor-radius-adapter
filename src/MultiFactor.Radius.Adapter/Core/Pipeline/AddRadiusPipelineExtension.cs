using Microsoft.Extensions.DependencyInjection;
using MultiFactor.Radius.Adapter.Server.Pipeline;
using System;

namespace MultiFactor.Radius.Adapter.Core.Pipeline;

public static class AddRadiusPipelineExtension
{
    public static IServiceCollection AddRadiusPipeline(this IServiceCollection services, Action<IRadiusPipelineBuilder> buildPipeline = null)
    {
        var builder = new RadiusPipelineBuilder(services);
        buildPipeline?.Invoke(builder);

        var pipelineDelegate = builder.BuildPipelineDelegate();
        services.AddSingleton(pipelineDelegate);
        services.AddSingleton<IRadiusPipeline, RadiusPipeline>();
        return services;
    }
}
