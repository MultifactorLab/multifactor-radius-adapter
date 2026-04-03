using Microsoft.Extensions.DependencyInjection;
using Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.FirstFactor.Ports;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Features.PacketHandler.UseCases.FirstFactor;

public static class Module
{
    public static IServiceCollection AddCheckConnectionInfra(this IServiceCollection services)
    {
        services.AddTransient<ICheckConnection, CheckConnection>();
        return services;
    }
}