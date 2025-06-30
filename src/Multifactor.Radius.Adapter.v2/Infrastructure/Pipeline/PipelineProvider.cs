using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Multifactor.Core.Ldap.LangFeatures;
using Multifactor.Radius.Adapter.v2.Core.Configuration.Client;
using Multifactor.Radius.Adapter.v2.Core.Configuration.Service;
using Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline.Builder;
using Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline.Steps;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline;

public class PipelineProvider : IPipelineProvider
{
    private readonly Dictionary<string, IRadiusPipeline> _pipelines = new();
    
    public PipelineProvider(IServiceConfiguration configuration, IPipelineConfigurationFactory pipelineConfigurationFactory, IServiceProvider serviceProvider, ILogger<IPipelineProvider> logger)
    {
        Throw.IfNull(configuration, nameof(configuration));
        Throw.IfNull(pipelineConfigurationFactory, nameof(pipelineConfigurationFactory));
        Throw.IfNull(serviceProvider, nameof(serviceProvider));
        
        logger.LogDebug($"Initializing pipelines.");
        
        foreach (var clientConfiguration in configuration.Clients)
        {
            var shouldLoadUserGroups = ShouldLoadUserGroups(clientConfiguration);
            var pipelineSettings = new PipelineStepsConfiguration(clientConfiguration.Name, clientConfiguration.PreAuthnMode.Mode, shouldLoadUserGroups);
            var pipelineConfig = pipelineConfigurationFactory.CreatePipelineConfiguration(pipelineSettings);
            var pipeline = BuildPipeline(pipelineConfig, serviceProvider);
            var log = BuildLog(clientConfiguration.Name, pipelineConfig);
            _pipelines.TryAdd(clientConfiguration.Name, pipeline);
            logger.LogDebug(log);
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

    private string BuildLog(string configName, PipelineConfiguration pipelineConfiguration)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"Configuration: {configName}");
        builder.AppendLine("Steps:");
        for (int i = 0; i < pipelineConfiguration.PipelineStepsTypes.Length; i++)
        {
            builder.AppendLine($"{i+1}. {pipelineConfiguration.PipelineStepsTypes[i].Name}");
        }
        
        return builder.ToString();
    }
    
    private bool ShouldLoadUserGroups(IClientConfiguration config) => config
        .RadiusReplyAttributes
        .Values
        .SelectMany(x => x)
        .Any(x => x.IsMemberOf || x.UserGroupCondition.Count > 0);

}