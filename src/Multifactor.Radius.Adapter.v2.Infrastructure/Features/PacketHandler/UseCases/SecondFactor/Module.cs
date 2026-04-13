using Microsoft.Extensions.DependencyInjection;
using Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.SecondFactor.Multifactor.Ports;
using Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.SecondFactor.Ports;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Features.PacketHandler.UseCases.SecondFactor;

public static class Module
{
    public static IServiceCollection AddSecondFactorInfra(this IServiceCollection services)
    {
        services.AddSingleton<IAuthenticatedClientCache, AuthenticatedClientCache>();
        services.AddTransient<ICreateAccessRequest, CreateAccessRequest>();
        services.AddTransient<ISendChallenge, SendChallenge>();
        return services;
    }
}