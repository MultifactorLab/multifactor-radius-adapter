using Microsoft.Extensions.DependencyInjection;
using MultiFactor.Radius.Adapter.Configuration;
using MultiFactor.Radius.Adapter.Configuration.ConfigurationLoading;
using MultiFactor.Radius.Adapter.Configuration.Core;
using MultiFactor.Radius.Adapter.Core;
using MultiFactor.Radius.Adapter.Core.Http;
using MultiFactor.Radius.Adapter.Core.Ldap;
using MultiFactor.Radius.Adapter.Core.Pipeline;
using MultiFactor.Radius.Adapter.Core.Radius;
using MultiFactor.Radius.Adapter.Core.Radius.Attributes;
using MultiFactor.Radius.Adapter.Core.Serialization;
using MultiFactor.Radius.Adapter.HostedServices;
using MultiFactor.Radius.Adapter.Logging;
using MultiFactor.Radius.Adapter.Server;
using MultiFactor.Radius.Adapter.Server.FirstAuthFactorProcessing;
using MultiFactor.Radius.Adapter.Server.Pipeline;
using MultiFactor.Radius.Adapter.Services;
using MultiFactor.Radius.Adapter.Services.BindIdentityFormatting;
using MultiFactor.Radius.Adapter.Services.Ldap;
using MultiFactor.Radius.Adapter.Services.Ldap.Connection;
using MultiFactor.Radius.Adapter.Services.Ldap.MembershipVerification;
using MultiFactor.Radius.Adapter.Services.Ldap.ProfileLoading;
using MultiFactor.Radius.Adapter.Services.Ldap.UserGroupsReading;
using MultiFactor.Radius.Adapter.Services.MultiFactorApi;
using Serilog;
using System;
using System.Net.Http;

namespace MultiFactor.Radius.Adapter;

internal static class ServicesConfiguration
{
    public static IServiceCollection ConfigureServices(this IServiceCollection services, Action<IServiceCollection> configure = null)
    {
        if (services is null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        services.AddSingleton(prov => ApplicationVariablesFactory.Create());
        services.AddSingleton<IRootConfigurationProvider, DefaultRootConfigurationProvider>();

        services.AddSingleton<SerilogLoggerFactory>();
        services.AddSingleton(prov =>
        {
            var logger = prov.GetRequiredService<SerilogLoggerFactory>().CreateLogger();
            Log.Logger = logger;
            return logger;
        });

        services.AddSingleton<IClientConfigurationsProvider, DefaultClientConfigurationsProvider>();

        services.AddSingleton<IRadiusDictionary, RadiusDictionary>();

        services.AddSingleton<ServiceConfigurationFactory>();
        services.AddSingleton<ClientConfigurationFactory>();
        services.AddSingleton(prov =>
        {
            var config = prov.GetRequiredService<ServiceConfigurationFactory>().CreateConfig();
            config.Validate();
            return config;
        });

        services.AddMemoryCache();

        services.AddSingleton<IRadiusPacketParser, RadiusPacketParser>();
        services.AddSingleton<CacheService>();
        services.AddSingleton<IMultiFactorApiClient, MultiFactorApiClient>();
        services.AddSingleton<RadiusServer>();
        services.AddSingleton<RadiusContextFactory>();
        services.AddSingleton<IChallengeProcessor, ChallengeProcessor>();

        services.AddSingleton<IFirstAuthFactorProcessor, LdapFirstAuthFactorProcessor>();
        services.AddSingleton<IFirstAuthFactorProcessor, RadiusFirstAuthFactorProcessor>();
        services.AddSingleton<IFirstAuthFactorProcessor, DefaultFirstAuthFactorProcessor>();
        services.AddSingleton<IFirstAuthFactorProcessorProvider, FirstAuthFactorProcessorProvider>();

        services.AddSingleton<UserGroupsGetterProvider>();
        services.AddSingleton<UserGroupsSource>();
        services.AddSingleton<IUserGroupsGetter, ActiveDirectoryUserGroupsGetter>();
        services.AddSingleton<IUserGroupsGetter, DefaultUserGroupsGetter>();

        services.AddSingleton<BindIdentityFormatterFactory>();
        services.AddSingleton<LdapConnectionAdapterFactory>();
        services.AddSingleton<ProfileLoader>();
        services.AddSingleton<LdapService>();
        services.AddSingleton<MembershipVerifier>();
        services.AddSingleton<MembershipProcessor>();

        services.AddSingleton(prov => new RandomWaiter(prov.GetRequiredService<IServiceConfiguration>().InvalidCredentialDelay));
        services.AddSingleton<IAuthenticatedClientCache, AuthenticatedClientCache>();

        services.AddHostedService<ServerHost>();
        services.AddHostedService<Starter>();
        services.AddSingleton<IServerInfo, ServerInfo>();

        services.AddRadiusPipeline(builder =>
        {
            builder.Use<StatusServerMiddleware>();
            builder.Use<AccessRequestFilterMiddleware>();
            builder.Use<TransformUserNameMiddleware>();
            builder.Use<AccessChallengeMiddleware>();
            builder.Use<FirstFactorAuthenticationMiddleware>();
            builder.Use<Bypass2FaMidleware>();
            builder.Use<SecondFactorAuthenticationMiddleware>();
        });

        services.AddSingleton<IRadiusRequestPostProcessor, RadiusRequestPostProcessor>();
        services.AddSingleton<RadiusResponseSenderFactory>();

        services.AddSingleton<IJsonDataSerializer, SystemTextJsonDataSerializer>();
        services.AddSingleton<IHttpClientAdapter, HttpClientAdapter>();

        var clientBuilder = services.AddHttpClient(nameof(HttpClientAdapter), (prov, client) =>
        {
            var config = prov.GetRequiredService<IServiceConfiguration>();
            client.BaseAddress = new Uri(config.ApiUrl);
        }).ConfigurePrimaryHttpMessageHandler(prov =>
        {
            var config = prov.GetRequiredService<IServiceConfiguration>();
            var handler = new HttpClientHandler();

            if (string.IsNullOrWhiteSpace(config.ApiProxy)) return handler;

            if (!WebProxyFactory.TryCreateWebProxy(config.ApiProxy, out var webProxy))
            {
                throw new Exception("Unable to initialize WebProxy. Please, check whether multifactor-api-proxy URI is valid.");
            }
            handler.Proxy = webProxy;

            return handler;
        });

        configure?.Invoke(services);
        return services;
    }
}