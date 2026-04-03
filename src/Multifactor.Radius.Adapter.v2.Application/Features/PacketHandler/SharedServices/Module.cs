using Microsoft.Extensions.DependencyInjection;
using Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.SharedServices.ChallengeProcessor;

namespace Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.SharedServices;

public static class Module
{
    public static IServiceCollection AddSharedServices(this IServiceCollection services)
    {
        services.AddTransient<IChallengeProcessor, SecondFactorChallengeProcessor>();
        services.AddTransient<IChallengeProcessor, ChangePasswordChallengeProcessor>();
        services.AddSingleton<IChallengeProcessorProvider, ChallengeProcessorProvider>();
        return services;
    }
}