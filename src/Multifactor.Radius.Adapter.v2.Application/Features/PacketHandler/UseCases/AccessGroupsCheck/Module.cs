using Microsoft.Extensions.DependencyInjection;

namespace Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.AccessGroupsCheck;

public static class Module
{
    public static IServiceCollection AddAccessGroupsCheck(this IServiceCollection services)
    {
        return services.AddTransient<AccessGroupsCheckingStep>();
    }
}