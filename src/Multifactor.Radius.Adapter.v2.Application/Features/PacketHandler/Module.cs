using Microsoft.Extensions.DependencyInjection;
using Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.Pipeline;
using Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases;

namespace Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler;

public static class Module
{
    public static IServiceCollection AddPacketHandleFeatureApp(this IServiceCollection services)
    {
        services.AddUseCases();
        services.AddSingleton<IPipelineProvider, RadiusPipelineProvider>();
        services.AddSingleton<IRadiusPipelineFactory, RadiusPipelineFactory>();
        return services;
    }
}