using Microsoft.Extensions.DependencyInjection;
using Multifactor.Core.Ldap.LangFeatures;
using Multifactor.Radius.Adapter.v2.Core.Configuration.Service;
using Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline.Builder;
using Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline.Steps;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline;

public class PipelineProvider : IPipelineProvider
{
    private readonly Dictionary<string, IRadiusPipeline> _pipelines = new();
    
    public PipelineProvider(IServiceConfiguration configuration, IPipelineConfigurationFactory pipelineConfigurationFactory, IServiceProvider serviceProvider)
    {
        Throw.IfNull(configuration, nameof(configuration));
        Throw.IfNull(pipelineConfigurationFactory, nameof(pipelineConfigurationFactory));
        Throw.IfNull(serviceProvider, nameof(serviceProvider));
        
        foreach (var clientConfiguration in configuration.Clients)
        {
            var pipelineSettings = new PipelineStepsConfiguration(clientConfiguration.Name, clientConfiguration.PreAuthnMode.Mode, true); //TODO remove true;
            var pipelineConfig = pipelineConfigurationFactory.CreatePipelineConfiguration(pipelineSettings);
            var pipeline = BuildPipeline(pipelineConfig, serviceProvider);
            _pipelines.TryAdd(clientConfiguration.Name, pipeline);
        }
    }
    
    public IRadiusPipeline? GetRadiusPipeline(string key)
    {
        return _pipelines[key];
    }

    private IRadiusPipeline BuildPipeline(PipelineConfiguration pipelineConfiguration, IServiceProvider serviceProvider)
    {
        foreach (var stepType in pipelineConfiguration.PipelineStepsTypes)
        {
            if (!typeof(IRadiusPipelineStep).IsAssignableFrom(stepType))
            {
                throw new ArgumentException(
                    $"The type {stepType.FullName} does not implement {nameof(IRadiusPipelineStep)}");
            }
        }

        var builder = new PipelineBuilder();
        foreach (var type in pipelineConfiguration.PipelineStepsTypes)
        {
            var step = (IRadiusPipelineStep)serviceProvider.GetRequiredService(type);
            builder.AddPipelineStep(step);
        }

        return builder.Build()!;
    }
}