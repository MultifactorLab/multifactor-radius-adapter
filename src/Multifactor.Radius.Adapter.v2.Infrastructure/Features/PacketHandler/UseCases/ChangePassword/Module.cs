using Microsoft.Extensions.DependencyInjection;
using Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.SharedServices.ChallengeProcessor.Ports;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Features.PacketHandler.UseCases.ChangePassword;

public static class Module
{
    public static IServiceCollection AddChangePasswordInfra(this IServiceCollection services)
    {
        services.AddSingleton<IPasswordChangeCache, PasswordChangeCache>();
        services.AddTransient<IChangePassword, ChangePassword>();
        return services;
    }
}