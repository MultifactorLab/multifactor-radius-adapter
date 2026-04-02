using Microsoft.Extensions.DependencyInjection;

namespace Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.SecondFactor;

public static class Module
{
    public static IServiceCollection AddSecondFactor(this IServiceCollection services)
    {
        return services.AddTransient<SecondFactorStep>();
    }
}