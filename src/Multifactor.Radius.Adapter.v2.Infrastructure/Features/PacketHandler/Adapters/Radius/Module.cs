using Microsoft.Extensions.DependencyInjection;
using Multifactor.Radius.Adapter.v2.Application.SharedPorts;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Features.PacketHandler.Adapters.Radius;

internal static class Module
{
    public static IServiceCollection AddRadiusInfra(this IServiceCollection services)
    {
        services.AddSingleton<IRadiusAttributeParser, RadiusAttributeParser>();
        services.AddSingleton<IPacketParser, PacketParser>();
        services.AddSingleton<IRadiusPacketParser, RadiusPacketParser>();
        services.AddSingleton<IRadiusPacketBuilder, RadiusPacketBuilder>();
        services.AddTransient<IRadiusReplyAttributeService, RadiusReplyAttributeService>();
        services.AddTransient<IRadiusAttributeTypeConverter, RadiusAttributeTypeConverter>();
        services.AddSingleton<IRadiusPacketValidator, RadiusPacketValidator>();
        return services;
    }
}