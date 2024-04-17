using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using MultiFactor.Radius.Adapter.Configuration.Core;
using MultiFactor.Radius.Adapter.Core;
using MultiFactor.Radius.Adapter.Core.Ldap;
using MultiFactor.Radius.Adapter.Framework;
using MultiFactor.Radius.Adapter.Logging;
using MultiFactor.Radius.Adapter.Server.Pipeline.AccessChallenge;
using MultiFactor.Radius.Adapter.Services;
using MultiFactor.Radius.Adapter.Services.BindIdentityFormatting;
using MultiFactor.Radius.Adapter.Services.Ldap;
using MultiFactor.Radius.Adapter.Services.Ldap.Connection;
using MultiFactor.Radius.Adapter.Services.Ldap.MembershipVerification;
using MultiFactor.Radius.Adapter.Services.Ldap.Profile;
using MultiFactor.Radius.Adapter.Services.Ldap.UserGroupsReading;
using MultiFactor.Radius.Adapter.Services.MultiFactorApi;
using Serilog;
using System;
using System.Linq;

namespace MultiFactor.Radius.Adapter.Extensions;

internal static class ConfigureApplicationExtension
{
    public static RadiusHostApplicationBuilder ConfigureApplication(this RadiusHostApplicationBuilder builder, Action<IServiceCollection> configureServices = null)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        builder.Services.AddSingleton<IMultifactorApiAdapter, MultifactorApiAdapter>();
        builder.Services.AddSingleton<IMultifactorApiClient, MultifactorApiClient>();

        builder.Services.AddSingleton<ISecondFactorChallengeProcessor, SecondFactorChallengeProcessor>();

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

        builder.Services.AddHttpServices();

        configureServices?.Invoke(builder.Services);
        return builder;
    }

    public static RadiusHostApplicationBuilder AddLogging(this RadiusHostApplicationBuilder builder)
    {
        // temporary service provider.
        var services = new ServiceCollection();

        var appVarDescriptor = builder.InternalHostApplicationBuilder.Services.FirstOrDefault(x => x.ServiceType == typeof(ApplicationVariables))
            ?? throw new System.Exception($"Service type '{typeof(ApplicationVariables)}' was not found in the RadiusHostApplicationBuilder services");
        services.Add(appVarDescriptor);

        var rootConfigProvDescriptor = builder.InternalHostApplicationBuilder.Services.FirstOrDefault(x => x.ServiceType == typeof(IRootConfigurationProvider))
            ?? throw new System.Exception($"Service type '{typeof(IRootConfigurationProvider)}' was not found in the RadiusHostApplicationBuilder services");
        services.Add(rootConfigProvDescriptor);

        services.AddSingleton<SerilogLoggerFactory>();

        var prov = services.BuildServiceProvider();
        var logger = prov.GetRequiredService<SerilogLoggerFactory>().CreateLogger();

        Log.Logger = logger;
        builder.InternalHostApplicationBuilder.Logging.ClearProviders();
        builder.InternalHostApplicationBuilder.Logging.AddSerilog(logger);

        return builder;
    }

}