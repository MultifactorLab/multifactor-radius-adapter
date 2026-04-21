using Microsoft.Extensions.DependencyInjection;
using Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.FirstFactor.Ports;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Integrations.Radius;

public static class Module
{
    public static IServiceCollection AddRadiusIntegration(this IServiceCollection services)
    {
        services.AddSingleton<IRadiusClientFactory, RadiusClientFactory>();
        return services;
    }
}