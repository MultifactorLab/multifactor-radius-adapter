using Microsoft.Extensions.DependencyInjection;
using Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.LoadLdapForest.Port;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Features.PacketHandler.UseCases.LoadLdapForest;

public static class Module
{
    public static IServiceCollection AddLdapForestLoadInfra(this IServiceCollection services)
    {
        services.AddTransient<IForestCache, ForestCache>();
        services.AddTransient<ILoadLdapForest, LoadLdapForest>();
        return services;
    }
}