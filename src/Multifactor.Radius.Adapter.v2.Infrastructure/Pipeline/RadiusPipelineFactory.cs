using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Multifactor.Radius.Adapter.v2.Application.Configuration;
using Multifactor.Radius.Adapter.v2.Application.Configuration.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline;
using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.Steps;
using Multifactor.Radius.Adapter.v2.Application.Models.Enum;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline;

public class RadiusPipelineFactory : IRadiusPipelineFactory
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
    
    public IRadiusPipeline CreatePipeline(ClientConfiguration clientConfig)
    {
        var steps = CreatePipelineSteps(clientConfig);
        return new RadiusPipeline(steps);
    }
    
    private List<IRadiusPipelineStep> CreatePipelineSteps(ClientConfiguration clientConfig)
    {
        var steps = new List<IRadiusPipelineStep>();
        
        steps.Add(CreateStep<StatusServerFilteringStep>());
        steps.Add(CreateStep<IpWhiteListStep>());
        steps.Add(CreateStep<AccessRequestFilteringStep>());
        
        if (clientConfig.LdapServers?.Count > 0)
        {
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
        
        if (ShouldLoadUserGroups(clientConfig))
        {
            steps.Add(CreateStep<UserGroupLoadingStep>());
        }
        
        return steps;
    }
    
    private IRadiusPipelineStep CreateStep<TStep>() where TStep : IRadiusPipelineStep
    {
        return _serviceProvider.GetRequiredService<TStep>();
    }
    
    private bool ShouldLoadUserGroups(ClientConfiguration config) => config
        .ReplyAttributes
        .Values
        .SelectMany(x => x)
        .Any(x => x.IsMemberOf || x.UserGroupCondition.Count > 0);
}