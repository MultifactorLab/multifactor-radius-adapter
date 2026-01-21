using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Multifactor.Radius.Adapter.v2.Application.Configuration.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.Multifactor;
using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.AccessChallenge;
using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.FirstFactor;
using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.FirstFactor.BindNameFormat;
using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.Steps;
using Multifactor.Radius.Adapter.v2.Application.Features.Radius.Services;

namespace Multifactor.Radius.Adapter.v2.Application.Extensions;

public static class ApplicationExtensions
{
    public static void AddApplicationVariables(this IServiceCollection services)
    {
        var appVars = new ApplicationVariables
        {
            AppPath = Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory),
            AppVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString(),
            StartedAt = DateTime.Now
        };
        services.AddSingleton(appVars);
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
    
    public static void AddFirstFactor(this IServiceCollection services)
    {
        services.AddSingleton<IFirstFactorProcessorProvider, FirstFactorProcessorProvider>();
        services.AddTransient<IFirstFactorProcessor, LdapFirstFactorProcessor>();
        services.AddTransient<IFirstFactorProcessor, RadiusFirstFactorProcessor>();
        services.AddTransient<IFirstFactorProcessor, NoneFirstFactorProcessor>();
    }

    public static void AddChallenge(this IServiceCollection services)
    {
        services.AddTransient<IChallengeProcessor, SecondFactorChallengeProcessor>();
        services.AddTransient<IChallengeProcessor, ChangePasswordChallengeProcessor>();
        services.AddSingleton<IChallengeProcessorProvider, ChallengeProcessorProvider>();
    }
    
    public static void AddPipelineSteps(this IServiceCollection services)
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
    
    public static void AddAppServices(this IServiceCollection services)
    {
        services.AddTransient<MultifactorApiService>();
        services.AddTransient<IRadiusPacketProcessor, RadiusPacketProcessor>();
        AddLdapBindNameFormation(services);
    }
}