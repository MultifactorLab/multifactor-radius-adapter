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
using MultiFactor.Radius.Adapter.Server.Pipeline.AccessRequestFilter;
using MultiFactor.Radius.Adapter.Server.Pipeline.FirstFactorAuthentication;
using MultiFactor.Radius.Adapter.Server.Pipeline.PreSecondFactorAuthentication;
using MultiFactor.Radius.Adapter.Server.Pipeline.SecondFactorAuthentication;
using MultiFactor.Radius.Adapter.Server.Pipeline.StatusServer;

namespace MultiFactor.Radius.Adapter.Extensions;

internal static class RadiusHostApplicationBuilderExtensions
{
    public static void ConfigureApplication(this RadiusHostApplicationBuilder builder, Action<IServiceCollection> configureServices = null)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        builder.Services.AddSingleton<IMultifactorApiAdapter, MultifactorApiAdapter>();
        builder.Services.AddSingleton<IMultifactorApiClient, MultifactorApiClient>();
        
        builder.Services.AddSingleton<IChallengeProcessor, SecondFactorChallengeProcessor>();
        builder.Services.AddSingleton<IChallengeProcessor, ChangePasswordChallengeProcessor>();
        builder.Services.AddSingleton<IChallengeProcessorProvider, ChallengeProcessorProvider>();
        
        builder.Services.AddFirstAuthFactorProcessing();

        builder.Services.AddSingleton<UserGroupsGetterProvider>();
        builder.Services.AddSingleton<UserGroupsSource>();
        builder.Services.AddSingleton<IUserGroupsGetter, ActiveDirectoryUserGroupsGetter>();
        builder.Services.AddSingleton<IUserGroupsGetter, DefaultUserGroupsGetter>();

        builder.Services.AddSingleton<ProfileLoader>();
        builder.Services.AddSingleton<ILdapService, LdapService>();
        builder.Services.AddSingleton<MembershipVerifier>();
        builder.Services.AddSingleton<MembershipProcessor>();

        builder.Services.AddSingleton<IAuthenticatedClientCache, AuthenticatedClientCache>();

        builder.Services.AddSingleton<DataProtectionService>();

        builder.Services.AddHttpServices();
        configureServices?.Invoke(builder.Services);
    }

    public static void AddLogging(this RadiusHostApplicationBuilder builder)
    {
        var rootConfig = RootConfigurationProvider.GetRootConfiguration();
        var logger = SerilogLoggerFactory.CreateLogger(rootConfig);
        Log.Logger = logger;

        builder.InternalHostApplicationBuilder.Logging.ClearProviders();
        builder.InternalHostApplicationBuilder.Logging.AddSerilog(logger);
    }

    public static void AddMiddlewares(this RadiusHostApplicationBuilder builder)
    {
        builder.UseMiddleware<StatusServerMiddleware>();
        builder.UseMiddleware<AccessRequestFilterMiddleware>();
        builder.UseMiddleware<AccessChallengeMiddleware>();
        builder.UseMiddleware<AnonymousFirstFactorAuthenticationMiddleware>();
        builder.UseMiddleware<PreSecondFactorAuthenticationMiddleware>();
        builder.UseMiddleware<FirstFactorAuthenticationMiddleware>();
        builder.UseMiddleware<SecondFactorAuthenticationMiddleware>();
    }
}