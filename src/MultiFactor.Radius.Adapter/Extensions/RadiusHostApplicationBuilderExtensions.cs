using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MultiFactor.Radius.Adapter.Core.Framework;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration.ConfigurationLoading;
using MultiFactor.Radius.Adapter.Infrastructure.Logging;
using MultiFactor.Radius.Adapter.Server.Pipeline.AccessChallenge;
using MultiFactor.Radius.Adapter.Services;
using MultiFactor.Radius.Adapter.Services.Ldap;
using MultiFactor.Radius.Adapter.Services.Ldap.MembershipVerification;
using MultiFactor.Radius.Adapter.Services.Ldap.Profile;
using MultiFactor.Radius.Adapter.Services.Ldap.UserGroupsReading;
using MultiFactor.Radius.Adapter.Services.MultiFactorApi;
using Serilog;
using System;

namespace MultiFactor.Radius.Adapter.Extensions;

internal static class RadiusHostApplicationBuilderExtensions
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
        var rootConfig = RootConfigurationProvider.GetRootConfiguration();
        var logger = SerilogLoggerFactory.CreateLogger(rootConfig);
        Log.Logger = logger;

        builder.InternalHostApplicationBuilder.Logging.ClearProviders();
        builder.InternalHostApplicationBuilder.Logging.AddSerilog(logger);

        return builder;
    }
}