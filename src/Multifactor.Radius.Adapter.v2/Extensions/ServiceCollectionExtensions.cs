using System.Runtime.InteropServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Multifactor.Core.Ldap.Schema;
using Multifactor.Radius.Adapter.v2.Core.Configuration.Client.Build;
using Multifactor.Radius.Adapter.v2.Core.Configuration.Service;
using Multifactor.Radius.Adapter.v2.Core.Configuration.Service.Build;
using Multifactor.Radius.Adapter.v2.Core.FirstFactor;
using Multifactor.Radius.Adapter.v2.Core.Radius.Attributes;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configuration.RadiusAdapter.Build;
using Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline;
using Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline.Builder;
using Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline.Steps;
using Multifactor.Radius.Adapter.v2.Server.Udp;
using Multifactor.Radius.Adapter.v2.Services.DataProtection;
using Multifactor.Radius.Adapter.v2.Services.Ldap;

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

    public static void AddPipelines(this IServiceCollection services)
    {
        services.AddPipelineSteps();
        services.AddSingleton<IPipelineProvider, PipelineProvider>();
        services.AddSingleton<IPipelineConfigurationFactory, PipelineConfigurationFactory>();
        services.AddTransient<IPipelineBuilder, PipelineBuilder>();
    }

    public static void AddConfiguration(this IServiceCollection services)
    {
        services.AddSingleton<IClientConfigurationsProvider, DefaultClientConfigurationsProvider>();

        services.AddSingleton<IServiceConfigurationFactory, ServiceConfigurationFactory>();
        services.AddSingleton<IClientConfigurationFactory, ClientConfigurationFactory>();

        services.AddSingleton(prov =>
        {
            var rootConfig = RadiusAdapterConfigurationProvider.GetRootConfiguration();
            var factory = prov.GetRequiredService<IServiceConfigurationFactory>();

            var config = factory.CreateConfig(rootConfig);

            return config;
        });
    }

    public static void AddUdpClient(this IServiceCollection services)
    {
        services.AddSingleton<IUdpClient>(prov =>
        {
            var config = prov.GetService<IServiceConfiguration>();
            if (config == null)
                throw new NullReferenceException("Provided service configuration is null");
            return new CustomUdpClient(config.ServiceServerEndpoint);
        });
    }

    public static void AddRadiusDictionary(this IServiceCollection services)
    {
        services.AddSingleton<RadiusDictionary>();
        services.AddSingleton<IRadiusDictionary, RadiusDictionary>(prov =>
        {
            var dict = prov.GetRequiredService<RadiusDictionary>();
            dict.Read();
            return dict;
        });
    }

    public static void AddFirstFactor(this IServiceCollection services)
    {
        services.AddSingleton<IFirstFactorProcessorProvider, FirstFactorProcessorProvider>();
        services.AddTransient<IFirstFactorProcessor, LdapFirstFactorProcessor>();
        services.AddTransient<IFirstFactorProcessor, RadiusFirstFactorProcessor>();
        services.AddTransient<IFirstFactorProcessor, NoneFirstFactorProcessor>();
    }

    private static void AddPipelineSteps(this IServiceCollection services)
    {
        services.AddTransient<AccessChallengeStep>();
        services.AddTransient<AccessRequestFilteringStep>();
        services.AddTransient<CheckingMembershipStep>();
        services.AddTransient<FirstFactorStep>();
        services.AddTransient<ProfileLoadingStep>();
        services.AddTransient<SecondFactorStep>();
        services.AddTransient<StatusServerFilteringStep>();
    }

    public static void AddLdapSchemaLoader(this IServiceCollection services)
    {
        services.AddSingleton<LdapSchemaLoader>();
        services.AddSingleton<ILdapSchemaLoader, CustomLdapSchemaLoader>();
    }

    public static void AddDataProtection(this IServiceCollection services)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            services.AddTransient<IDataProtectionService, WindowsProtectionService>();
        else
            services.AddTransient<IDataProtectionService, LinuxProtectionService>();
    }
}