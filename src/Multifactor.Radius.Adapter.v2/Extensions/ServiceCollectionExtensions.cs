using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Multifactor.Radius.Adapter.v2.Core.Configuration.Client.Build;
using Multifactor.Radius.Adapter.v2.Core.Configuration.Service.Build;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configuration.RadiusAdapter.Build;
using Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline;
using Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline.Builder;
using Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline.Steps;

namespace Multifactor.Radius.Adapter.v2.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddPipeline(
        this IServiceCollection services,
        string pipelineKey,
        PipelineConfiguration pipelineConfiguration)
    {
        if (pipelineConfiguration is null)
            throw new ArgumentNullException(nameof(pipelineConfiguration));

        foreach (var stepType in pipelineConfiguration.PipelineStepsTypes)
        {
            if (!typeof(IRadiusPipelineStep).IsAssignableFrom(stepType))
            {
                throw new ArgumentException(
                    $"The type {stepType.FullName} does not implement {nameof(IRadiusPipelineStep)}");
            }

            services.TryAddTransient(stepType);
        }

        services.TryAddTransient<IPipelineBuilder, PipelineBuilder>();
        services.AddKeyedSingleton<IRadiusPipeline>(pipelineKey, (serviceProvider, x) =>
        {
            var pipelineBuilder = serviceProvider.GetRequiredService<IPipelineBuilder>();
            foreach (var type in pipelineConfiguration.PipelineStepsTypes)
            {
                var step = (IRadiusPipelineStep)serviceProvider.GetRequiredService(type);
                pipelineBuilder.AddPipelineStep(step);
            }

            return pipelineBuilder.Build()!;
        });
    }
    
    public static void AddConfiguration(this IServiceCollection services)
    {
        services.AddSingleton<IClientConfigurationsProvider, DefaultClientConfigurationsProvider>();

        services.AddSingleton<ServiceConfigurationFactory>();
        services.AddSingleton<ClientConfigurationFactory>();

        services.AddSingleton(prov =>
        {
            var rootConfig = RadiusAdapterConfigurationProvider.GetRootConfiguration();
            var factory = prov.GetRequiredService<ServiceConfigurationFactory>();

            var config = factory.CreateConfig(rootConfig);

            return config;
        });
    }
}