using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Multifactor.Radius.Adapter.v2.Application.Core.Enum;
using Multifactor.Radius.Adapter.v2.Application.Core.Models.Abstractions;
using Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases;
using Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.AccessChallenge;
using Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.AccessGroupsCheck;
using Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.AccessRequestFilter;
using Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.FirstFactor;
using Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.IpWhiteList;
using Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.LoadLdapForest;
using Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.LoadProfile;
using Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.LoadSchema;
using Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.PreAuthCheck;
using Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.PreAuthPostCheck;
using Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.SecondFactor;
using Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.StatusServerFilter;
using Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.UserNameValidation;

namespace Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.Pipeline;

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
            steps.Add(CreateStep<LoadLdapSchemaStep>());
            steps.Add(CreateStep<UserNameValidationStep>());
            steps.Add(CreateStep<ProfileLoadingStep>());
            steps.Add(CreateStep<AccessGroupsCheckingStep>());
        }
        
        steps.Add(CreateStep<AccessChallengeStep>());
        
        if (clientConfig.PreAuthenticationMethod != PreAuthMode.None)
        {
            steps.Add(CreateStep<PreAuthCheckStep>());
            steps.Add(CreateStep<SecondFactorStep>());
            steps.Add(CreateStep<PreAuthPostCheckStep>());
            steps.Add(CreateStep<FirstFactorStep>());
        }
        else
        {
            steps.Add(CreateStep<FirstFactorStep>());
            steps.Add(CreateStep<SecondFactorStep>());
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
}