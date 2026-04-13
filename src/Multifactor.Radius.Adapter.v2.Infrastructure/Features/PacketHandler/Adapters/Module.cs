using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Multifactor.Radius.Adapter.v2.Application.Core.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.Ports;
using Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.LoadSchema.Ports;
using Multifactor.Radius.Adapter.v2.Application.SharedPorts;
using Multifactor.Radius.Adapter.v2.Infrastructure.Features.PacketHandler.Adapters.Radius;
using Multifactor.Radius.Adapter.v2.Infrastructure.Features.PacketHandler.UseCases.LoadSchema;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Features.PacketHandler.Adapters;

internal static class Module
{
    public static IServiceCollection AddPacketHandlerAdapters(this IServiceCollection services)
    {
        services.AddRadiusInfra();
        services.AddSingleton<IPacketKeyCache, PacketKeyCache>();
        services.AddTransient<IChallengeContextCache, ChallengeContextCache>();
        services.AddTransient<ICheckMembership, CheckMembership>();
        services.AddTransient<ILoadLdapSchema, LoadLdapSchema>();
        services.AddTransient<IResponseSender, AdapterResponseSender>();
        services.AddTransient<IPacketSerializer, PacketSerializer>();
        services.AddUdpClient();
        return services;
    }
    private static IServiceCollection AddUdpClient(this IServiceCollection services)
    {        
        return services.AddSingleton<IUdpClient>(serviceProvider =>
        {
            var config = serviceProvider.GetRequiredService<ServiceConfiguration>();
            var endpoint = config.RootConfiguration.AdapterServerEndpoint;
            var logger = serviceProvider.GetService<ILogger<CustomUdpClient>>();
            return new CustomUdpClient(endpoint, logger!);
        });
    }
}