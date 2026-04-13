using Microsoft.Extensions.DependencyInjection;
using Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.SecondFactor.Multifactor;

namespace Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.SecondFactor;

public static class Module
{
    public static IServiceCollection AddSecondFactor(this IServiceCollection services)
    {
        services.AddSingleton<MultifactorApiService>();
        return services.AddTransient<SecondFactorStep>();
    }
}