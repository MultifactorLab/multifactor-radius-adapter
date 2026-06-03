using Microsoft.Extensions.DependencyInjection;

namespace Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.DenyGroupsCheck;

public static class Module
{
    public static IServiceCollection AddDenyGroupsCheck(this IServiceCollection services)
    {
        return services.AddTransient<DenyGroupsCheckingStep>();
    }
}