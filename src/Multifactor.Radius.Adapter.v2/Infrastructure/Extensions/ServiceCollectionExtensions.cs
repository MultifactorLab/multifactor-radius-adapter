using System.Security.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http.Resilience;
using Multifactor.Core.Ldap.Connection.LdapConnectionFactory;
using Multifactor.Core.Ldap.LdapGroup.Load;
using Multifactor.Core.Ldap.LdapGroup.Membership;
using Multifactor.Core.Ldap.Schema;
using Multifactor.Radius.Adapter.v2.Application.FirstFactor;
using Multifactor.Radius.Adapter.v2.Application.FirstFactor.BindNameFormat;
using Multifactor.Radius.Adapter.v2.Application.Pipeline.Steps;
using Multifactor.Radius.Adapter.v2.Application.Pipeline.Steps.Interfaces;
using Multifactor.Radius.Adapter.v2.Domain.Ldap;
using Multifactor.Radius.Adapter.v2.Domain.MultifactorApi.Interfaces;
using Multifactor.Radius.Adapter.v2.Domain.Radius.Attributes;
using Multifactor.Radius.Adapter.v2.Domain.RadiusAdapter;
using Multifactor.Radius.Adapter.v2.Infrastructure.AuthenticatedClientCache;
using Multifactor.Radius.Adapter.v2.Infrastructure.Cache;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configuration.Client;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configuration.Client.Build;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configuration.Service;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configuration.Service.Build;
using Multifactor.Radius.Adapter.v2.Infrastructure.Http;
using Multifactor.Radius.Adapter.v2.Infrastructure.Ldap;
using Multifactor.Radius.Adapter.v2.Infrastructure.Ldap.Interface;
using Multifactor.Radius.Adapter.v2.Infrastructure.Logging;
using Multifactor.Radius.Adapter.v2.Infrastructure.MultifactorApi;
using Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline;
using Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline.Configuration;
using Multifactor.Radius.Adapter.v2.Infrastructure.Radius;
using Multifactor.Radius.Adapter.v2.Infrastructure.Radius.Interfaces;
using Multifactor.Radius.Adapter.v2.Infrastructure.Server;
using Multifactor.Radius.Adapter.v2.Infrastructure.Server.Interfaces;
using Polly;
using Serilog;
using ILdapConnectionFactory = Multifactor.Radius.Adapter.v2.Domain.Ldap.Interfaces.ILdapConnectionFactory;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
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

                if (!WebProxyFactory.TryCreateProxy(config.ApiProxy, out var webProxy))
                    throw new Exception(
                        "Unable to initialize WebProxy. Please, check whether multifactor-api-proxy URI is valid.");

                handler.Proxy = webProxy;

                return handler;
            })
            .AddResilienceHandler("mf-api-pipeline", x =>
            {
                x.AddRetry(new HttpRetryStrategyOptions
                {
                    MaxRetryAttempts = 2,
                    Delay = TimeSpan.FromSeconds(1),
                    BackoffType = DelayBackoffType.Exponential
                });
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

    public static void AddLdapSchemaLoader(this IServiceCollection services)
    {
        services.AddSingleton<LdapSchemaLoader>();
        services.AddTransient<ILdapSchemeLoaderWrapper, LdapSchemaLoaderWrapper>();
        services.AddSingleton<ILdapSchemaLoader, CustomLdapSchemaLoader>();
    }


    public static void AddServices(this IServiceCollection services)
    {
        services.AddTransient<IRadiusPacketService, RadiusPacketService>();
        services.AddSingleton<IRadiusClientFactory, RadiusClientFactory>();

        services.AddSingleton<IAuthenticatedClientCache, AuthenticatedClientCache.AuthenticatedClientCache>();

        services.AddSingleton(LdapConnectionFactory.Create());
        services.AddSingleton<ILdapConnectionFactory, CustomLdapConnectionFactory>((prov) => new CustomLdapConnectionFactory());

        services.AddSingleton<ILdapGroupLoaderFactory, LdapGroupLoaderFactory>();
        services.AddSingleton<IMembershipCheckerFactory, MembershipCheckerFactory>();
        services.AddTransient<ILdapGroupService, LdapGroupService>();
        
        services.AddTransient<ILdapProfileService, LdapProfileService>();

        services.AddTransient<IMultifactorApi, MultifactorApi.MultifactorApi>();
        services.AddSingleton<IAuthenticatedClientCache, AuthenticatedClientCache.AuthenticatedClientCache>();
        services.AddTransient<IMultifactorApiService, MultifactorApiService>();

        services.AddTransient<IRadiusReplyAttributeService, RadiusReplyAttributeService>();
        services.AddTransient<IRadiusAttributeTypeConverter, RadiusAttributeTypeConverter>();
        services.AddTransient<IRadiusPacketProcessor, RadiusPacketProcessor>();
        services.AddSingleton<ICacheService, CacheService>();
        AddLdapBindNameFormation(services);
    }

    public static void AddAdapterLogging(this IServiceCollection services)
    {
        var rootConfig = RadiusAdapterConfigurationProvider.GetRootConfiguration();
        var logger = SerilogLoggerFactory.CreateLogger(rootConfig);
        Log.Logger = logger;

        services.AddSerilog();
    }

    private static void AddLdapBindNameFormation(IServiceCollection services)
    {
        services.AddSingleton<ILdapBindNameFormatterProvider, LdapBindNameFormatterProvider>();
        services.AddTransient<ILdapBindNameFormatter, ActiveDirectoryFormatter>();
        services.AddTransient<ILdapBindNameFormatter, FreeIpaFormatter>();
        services.AddTransient<ILdapBindNameFormatter, MultiDirectoryFormatter>();
        services.AddTransient<ILdapBindNameFormatter, OpenLdapFormatter>();
        services.AddTransient<ILdapBindNameFormatter, SambaFormatter>();
    }
}