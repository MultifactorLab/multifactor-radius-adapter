using Microsoft.Extensions.DependencyInjection;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Features.PacketHandler.UseCases.ChangePassword;

public static class Module
{
    public static IServiceCollection AddChangePasswordInfra(this IServiceCollection services)
    {
        return services;
    }
}