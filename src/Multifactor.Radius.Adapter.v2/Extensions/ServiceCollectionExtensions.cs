using System.Runtime.InteropServices;
using System.Security.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Multifactor.Core.Ldap.Connection.LdapConnectionFactory;
using Multifactor.Core.Ldap.LdapGroup.Load;
using Multifactor.Core.Ldap.LdapGroup.Membership;
using Multifactor.Core.Ldap.Schema;
using Multifactor.Radius.Adapter.v2.Core.AccessChallenge;
using Multifactor.Radius.Adapter.v2.Core.Configuration.Client.Build;
using Multifactor.Radius.Adapter.v2.Core.Configuration.Service;
using Multifactor.Radius.Adapter.v2.Core.Configuration.Service.Build;
using Multifactor.Radius.Adapter.v2.Core.FirstFactor;
using Multifactor.Radius.Adapter.v2.Core.Ldap;
using Multifactor.Radius.Adapter.v2.Core.Radius.Attributes;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configuration.RadiusAdapter.Build;
using Multifactor.Radius.Adapter.v2.Infrastructure.Http;
using Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline;
using Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline.Builder;
using Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline.Steps;
using Multifactor.Radius.Adapter.v2.Server.Udp;
using Multifactor.Radius.Adapter.v2.Services.AuthenticatedClientCache;
using Multifactor.Radius.Adapter.v2.Services.DataProtection;
using Multifactor.Radius.Adapter.v2.Services.Ldap;
using Multifactor.Radius.Adapter.v2.Services.LdapForest;
using Multifactor.Radius.Adapter.v2.Services.MultifactorApi;
using Multifactor.Radius.Adapter.v2.Services.NetBios;
using Multifactor.Radius.Adapter.v2.Services.Radius;
using ILdapConnectionFactory = Multifactor.Radius.Adapter.v2.Core.Ldap.ILdapConnectionFactory;

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

    public static void AddMultifactorHttpClient(this IServiceCollection services)
    {
        services.AddSingleton<IHttpClient, MultifactorHttpClient>();
        services.AddHttpClient(nameof(MultifactorHttpClient), (prov, client) =>
        {
            var config = prov.GetRequiredService<IServiceConfiguration>();
            client.BaseAddress = new Uri(config.ApiUrl);
            client.Timeout = config.ApiTimeout;
        }).ConfigurePrimaryHttpMessageHandler(prov =>
        {
            var config = prov.GetRequiredService<IServiceConfiguration>();
            var handler = new HttpClientHandler
            {
                MaxConnectionsPerServer = 100,
                SslProtocols = SslProtocols.Tls13 | SslProtocols.Tls12
            };
            
            if (string.IsNullOrWhiteSpace(config.ApiProxy))
                return handler;
            
            if (!WebProxyFactory.TryCreateWebProxy(config.ApiProxy, out var webProxy))
                throw new Exception("Unable to initialize WebProxy. Please, check whether multifactor-api-proxy URI is valid.");

            handler.Proxy = webProxy;

            return handler;
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

    public static void AddChallenge(this IServiceCollection services)
    {
        services.AddTransient<IChallengeProcessor, SecondFactorChallengeProcessor>();
        services.AddTransient<IChallengeProcessor, ChangePasswordChallengeProcessor>();
        services.AddSingleton<IChallengeProcessorProvider, ChallengeProcessorProvider>();
    }

    public static void AddServices(this IServiceCollection services)
    {
        services.AddTransient<IRadiusPacketService, RadiusPacketService>();
        
        services.AddSingleton<IAuthenticatedClientCache, AuthenticatedClientCache>();
        
        services.AddSingleton(LdapConnectionFactory.Create());
        services.AddSingleton<ILdapConnectionFactory, CustomLdapConnectionFactory>((prov) => new CustomLdapConnectionFactory());
        
        services.AddSingleton<ILdapGroupLoaderFactory, LdapGroupLoaderFactory>();
        services.AddSingleton<IMembershipCheckerFactory, MembershipCheckerFactory>();
        services.AddTransient<ILdapGroupService, LdapGroupService>();
        
        services.AddSingleton<IForestMetadataCache, ForestMetadataCache>();
        services.AddTransient<ILdapProfileService, LdapProfileService>();

        services.AddTransient<IMultifactorApi, MultifactorApi>();
        services.AddSingleton<IAuthenticatedClientCache, AuthenticatedClientCache>();
        services.AddTransient<IMultifactorApiService, MultifactorApiService>();
        services.AddTransient<INetBiosService, NetBiosService>();

        services.AddTransient<IRadiusReplyAttributeService, RadiusReplyAttributeService>();
    }
}