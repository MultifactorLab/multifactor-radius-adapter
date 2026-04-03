using Microsoft.Extensions.DependencyInjection;
using Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler;
using Multifactor.Radius.Adapter.v2.Infrastructure.Features.PacketHandler;

namespace Multifactor.Radius.Adapter.v2.Features.PacketHandle;

public static class Module
{
    public static void AddPacketHandleFeature(this IServiceCollection services)
    {
        services.AddPacketHandleFeatureApp();
        services.AddPacketHandleFeatureInfra();
        services.AddSingleton<IRadiusPacketProcessor, RadiusPacketProcessor>();
        services.AddSingleton<IRadiusUdpAdapter, RadiusUdpAdapter>();
    }
}