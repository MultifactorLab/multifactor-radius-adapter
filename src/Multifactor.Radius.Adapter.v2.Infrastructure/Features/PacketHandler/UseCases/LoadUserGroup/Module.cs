using Microsoft.Extensions.DependencyInjection;
using Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.LoadUserGroup.Ports;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Features.PacketHandler.UseCases.LoadUserGroup;

public static class Module
{
    public static IServiceCollection AddLoadUserGroupInfra(this IServiceCollection services)
    {
        services.AddTransient<ILoadGroups, LoadGroups>();
        return services;
    }
}