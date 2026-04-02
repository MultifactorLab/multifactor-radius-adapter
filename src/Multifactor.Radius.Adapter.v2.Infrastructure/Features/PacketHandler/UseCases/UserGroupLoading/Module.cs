using Microsoft.Extensions.DependencyInjection;
using Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.UserGroupLoading.Ports;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Features.PacketHandler.UseCases.UserGroupLoading;

public static class Module
{
    public static IServiceCollection AddLoadGroupsInfra(this IServiceCollection services)
    {
        services.AddTransient<ILoadGroups, LoadGroups>();
        return services;
    }
}