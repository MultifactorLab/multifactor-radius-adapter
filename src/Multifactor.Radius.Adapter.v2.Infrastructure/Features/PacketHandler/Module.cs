using Microsoft.Extensions.DependencyInjection;
using Multifactor.Radius.Adapter.v2.Infrastructure.Features.PacketHandler.Adapters;
using Multifactor.Radius.Adapter.v2.Infrastructure.Features.PacketHandler.UseCases;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Features.PacketHandler;

public static class Module
{
    public static IServiceCollection AddPacketHandleFeatureInfra(this IServiceCollection services)
    {
        services.AddPacketHandlerAdapters();
        services.AddUseCasesInfra();
        return services;
    }
}