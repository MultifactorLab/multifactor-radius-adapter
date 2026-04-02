using Microsoft.Extensions.DependencyInjection;
using Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.Ports;
using Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.FirstFactor.Ports;
using Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.SecondFactor.Ports;
using Multifactor.Radius.Adapter.v2.Infrastructure.Features.PacketHandler.UseCases.SecondFactor;
using Multifactor.Radius.Adapter.v2.Infrastructure.Radius_remove_.Builders;
using Multifactor.Radius.Adapter.v2.Infrastructure.Radius_remove_.Client;
using Multifactor.Radius.Adapter.v2.Infrastructure.Radius_remove_.Parsers;
using Multifactor.Radius.Adapter.v2.Infrastructure.Radius_remove_.Sender;
using Multifactor.Radius.Adapter.v2.Infrastructure.Radius_remove_.Services;
using Multifactor.Radius.Adapter.v2.Infrastructure.Radius_remove_.Validators;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Extensions_remove_;

public static class InfrastructureExtensions
{
    public static void AddRadiusUdpClient(this IServiceCollection services)
    {

        services.AddSingleton<IRadiusAttributeParser, RadiusAttributeParser>();
        services.AddSingleton<IRadiusPacketParser, RadiusPacketParser>();
    }
    
    public static void AddInfraServices(this IServiceCollection services)
    {
        services.AddSingleton<IAuthenticatedClientCache, AuthenticatedClientCache>();
        services.AddSingleton<IRadiusPacketBuilder, RadiusPacketBuilder>();
        services.AddSingleton<IRadiusClientFactory, RadiusClientFactory>();
        services.AddTransient<IRadiusReplyAttributeService, RadiusReplyAttributeService>();
        services.AddTransient<IRadiusAttributeTypeConverter, RadiusAttributeTypeConverter>();
        services.AddSingleton<IRadiusPacketValidator, RadiusPacketValidator>();
    }

    public static void AddResponseSender(this IServiceCollection services)
    {
        services.AddTransient<IResponseSender, AdapterResponseSender>();
    }
}