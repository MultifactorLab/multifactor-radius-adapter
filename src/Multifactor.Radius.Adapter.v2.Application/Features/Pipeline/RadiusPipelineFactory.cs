using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Multifactor.Radius.Adapter.v2.Application.Core.Enum;
using Multifactor.Radius.Adapter.v2.Application.Core.Models.Abstractions;
using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.Steps;

namespace Multifactor.Radius.Adapter.v2.Application.Features.Pipeline;

public interface IRadiusPipelineFactory
{
    IRadiusPipeline CreatePipeline(IClientConfiguration clientConfig);
}

internal sealed class RadiusPipelineFactory : IRadiusPipelineFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<IRadiusPipelineFactory> _logger;
    
    public RadiusPipelineFactory(
        IServiceProvider serviceProvider,
        ILogger<IRadiusPipelineFactory> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }
    
    public IRadiusPipeline CreatePipeline(IClientConfiguration clientConfig)
    {
        var steps = CreatePipelineSteps(clientConfig);
        LogProviderCreated(clientConfig.Name, steps);
        return new RadiusPipeline(steps);
    }
    
    private List<IRadiusPipelineStep> CreatePipelineSteps(IClientConfiguration clientConfig)
    {
        var withLdap = clientConfig.LdapServers?.Count > 0;
        var steps = new List<IRadiusPipelineStep>
        {
            CreateStep<StatusServerFilteringStep>(),
            CreateStep<IpWhiteListStep>(),
            CreateStep<AccessRequestFilteringStep>()
        };

        if (withLdap)
        {
            if (OperatingSystem.IsWindows()) steps.Add(CreateStep<LoadLdapForestStep>());
            steps.Add(CreateStep<UserNameValidationStep>());
            steps.Add(CreateStep<LdapSchemaLoadingStep>());
            steps.Add(CreateStep<ProfileLoadingStep>());
            steps.Add(CreateStep<AccessGroupsCheckingStep>());
        }
        
        steps.Add(CreateStep<AccessChallengeStep>());
        
        if (clientConfig.PreAuthenticationMethod != PreAuthMode.None)
        {
            steps.Add(CreateStep<PreAuthCheckStep>());
            steps.Add(CreateStep<SecondFactorStep>());
            steps.Add(CreateStep<PreAuthPostCheck>());
            steps.Add(CreateStep<FirstFactorStep>());
        }
        else
        {
            steps.Add(CreateStep<FirstFactorStep>());
            steps.Add(CreateStep<SecondFactorStep>());
        }
        
        if (withLdap && ShouldLoadUserGroups(clientConfig))
        {
            steps.Add(CreateStep<UserGroupLoadingStep>());
        }
        
        return steps;
    }
    
    private IRadiusPipelineStep CreateStep<TStep>() where TStep : IRadiusPipelineStep
    {
        return _serviceProvider.GetRequiredService<TStep>();
    }
    
    private void LogProviderCreated(string configName, List<IRadiusPipelineStep> steps)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"Configuration: {configName}");
        builder.AppendLine("Steps:");
        for (var i = 0; i < steps.Count; i++)
        {
            builder.AppendLine($"{i+1}. {steps[i].GetType().Name}");
        }
        _logger.LogDebug(builder.ToString());
    }
    
    private static bool ShouldLoadUserGroups(IClientConfiguration config) => config
        .ReplyAttributes != null && config
        .ReplyAttributes
        .Values
        .SelectMany(x => x)
        .Any(x => x.IsMemberOf || x.UserGroupCondition.Count > 0);
}