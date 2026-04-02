using Microsoft.Extensions.DependencyInjection;
using Multifactor.Radius.Adapter.v2.Features.PacketHandle;

namespace Multifactor.Radius.Adapter.v2.Server;

public static class Module
{
    public static IServiceCollection AddServer(this IServiceCollection services)
    {
        services.AddPacketHandleFeature();
        services.AddSingleton<AdapterServer>();
        services.AddHostedService<ServerHost>();
        return services;
    }
}