using Microsoft.Extensions.DependencyInjection;
using Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler;

namespace Multifactor.Radius.Adapter.v2.Features.PacketHandle;

public static class Module
{
    public static void AddPacketHandleFeature(this IServiceCollection services)
    {
        services.AddPacketHandleFeatureApp();
        // services.AddPacketHandleFeatureInfra();TODO
        services.AddSingleton<IRadiusPacketProcessor, RadiusPacketProcessor>();
        services.AddSingleton<IRadiusUdpAdapter, RadiusUdpAdapter>();
    }
}