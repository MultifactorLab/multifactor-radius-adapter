﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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
using MultiFactor.Radius.Adapter.Server.Context;
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
using Serilog.Extensions.Logging;
using System;
using System.Net.Http;
using SerilogLoggerFactory = MultiFactor.Radius.Adapter.Logging.SerilogLoggerFactory;

namespace MultiFactor.Radius.Adapter;

internal static class AppConfiguration
{
    public static HostApplicationBuilder ConfigureApplication(this HostApplicationBuilder builder, Action<IServiceCollection> configureServices = null)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        AddLogging(builder, configureServices);

        builder.Services.AddSingleton<IMultiFactorApiClient, MultiFactorApiClient>();

        builder.Services.AddSingleton<IChallengeProcessor, ChallengeProcessor>();

        builder.Services.AddFirstAuthFactorProcessing();

        builder.Services.AddSingleton<UserGroupsGetterProvider>();
        builder.Services.AddSingleton<UserGroupsSource>();
        builder.Services.AddSingleton<IUserGroupsGetter, ActiveDirectoryUserGroupsGetter>();
        builder.Services.AddSingleton<IUserGroupsGetter, DefaultUserGroupsGetter>();

        builder.Services.AddSingleton<BindIdentityFormatterFactory>();
        builder.Services.AddSingleton<LdapConnectionAdapterFactory>();
        builder.Services.AddSingleton<ProfileLoader>();
        builder.Services.AddSingleton<LdapService>();
        builder.Services.AddSingleton<MembershipVerifier>();
        builder.Services.AddSingleton<MembershipProcessor>();

        builder.Services.AddSingleton<IAuthenticatedClientCache, AuthenticatedClientCache>();

        builder.Services.AddSingleton<IServerInfo, ServerInfo>();

        builder.Services.AddHttpServices();

        configureServices?.Invoke(builder.Services);
        return builder;
    }

    private static void AddFirstAuthFactorProcessing(this IServiceCollection services)
    {
        services.AddSingleton<IFirstAuthFactorProcessor, LdapFirstAuthFactorProcessor>();
        services.AddSingleton<IFirstAuthFactorProcessor, RadiusFirstAuthFactorProcessor>();
        services.AddSingleton<IFirstAuthFactorProcessor, DefaultFirstAuthFactorProcessor>();
        services.AddSingleton<IFirstAuthFactorProcessorProvider, FirstAuthFactorProcessorProvider>();
    }

    private static void AddLogging(HostApplicationBuilder builder, Action<IServiceCollection> configureServices = null)
    {
        // temporary service provider.
        var services = new ServiceCollection();

        services.AddSingleton(prov => ApplicationVariablesFactory.Create());
        services.AddSingleton<IRootConfigurationProvider, DefaultRootConfigurationProvider>();
        services.AddSingleton<SerilogLoggerFactory>();
        configureServices?.Invoke(services);

        var prov = services.BuildServiceProvider();
        var logger = prov.GetRequiredService<SerilogLoggerFactory>().CreateLogger();

        Log.Logger = logger;
        builder.Logging.ClearProviders();
        builder.Logging.AddSerilog(logger);
    }

    private static void AddHttpServices(this IServiceCollection services)
    {
        services.AddSingleton<IJsonDataSerializer, SystemTextJsonDataSerializer>();
        services.AddSingleton<IHttpClientAdapter, HttpClientAdapter>();
        services.AddHttpContextAccessor();
        services.AddTransient<MfTraceIdHeaderSetter>();

        services.AddHttpClient(nameof(HttpClientAdapter), (prov, client) =>
        {
            var config = prov.GetRequiredService<IServiceConfiguration>();
            client.BaseAddress = new Uri(config.ApiUrl);
            client.Timeout = config.ApiTimeout;
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
        })
        .AddHttpMessageHandler<MfTraceIdHeaderSetter>();
    }

}