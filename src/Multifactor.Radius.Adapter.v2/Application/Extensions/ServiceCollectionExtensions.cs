using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Multifactor.Radius.Adapter.v2.Application.Challenge;
using Multifactor.Radius.Adapter.v2.Application.Challenge.Interface;
using Multifactor.Radius.Adapter.v2.Application.Pipeline;
using Multifactor.Radius.Adapter.v2.Application.Pipeline.Builder;
using Multifactor.Radius.Adapter.v2.Application.Pipeline.Provider;
using Multifactor.Radius.Adapter.v2.Application.Pipeline.Steps;
using Multifactor.Radius.Adapter.v2.Application.Pipeline.Steps.Interfaces;
using Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline;
using Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline.Configuration;

namespace Multifactor.Radius.Adapter.v2.Application.Extensions;

public static class ServiceCollectionExtensions
{
    
    public static void AddChallenge(this IServiceCollection services)
    {
        services.AddTransient<IChallengeProcessor, SecondFactorChallengeProcessor>();
        services.AddTransient<IChallengeProcessor, ChangePasswordChallengeProcessor>();
        services.AddSingleton<IChallengeProcessorProvider, ChallengeProcessorProvider>();
    }
    
    public static void AddPipeline(
        this IServiceCollection services,
        string pipelineKey,
        PipelineConfiguration pipelineConfiguration)
    {
        ArgumentNullException.ThrowIfNull(pipelineConfiguration);

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

    public static void AddPipelines(this IServiceCollection services)
    {
        services.AddPipelineSteps();
        services.AddSingleton<IPipelineProvider, PipelineProvider>();
        services.AddSingleton<IPipelineConfigurationFactory, PipelineConfigurationFactory>();
        services.AddTransient<IPipelineBuilder, PipelineBuilder>();
    }
    
    private static void AddPipelineSteps(this IServiceCollection services)
    {
        services.AddTransient<StatusServerFilteringStep>();
        services.AddTransient<AccessRequestFilteringStep>();
        services.AddTransient<LdapSchemaLoadingStep>();
        services.AddTransient<ProfileLoadingStep>();
        services.AddTransient<AccessGroupsCheckingStep>();
        services.AddTransient<AccessChallengeStep>();
        services.AddTransient<FirstFactorStep>();
        services.AddTransient<SecondFactorStep>();
        services.AddTransient<PreAuthCheckStep>();
        services.AddTransient<PreAuthPostCheck>();
        services.AddTransient<UserGroupLoadingStep>();
        services.AddTransient<UserNameValidationStep>();
        services.AddTransient<IpWhiteListStep>();
    }

}