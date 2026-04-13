using Microsoft.Extensions.DependencyInjection;

namespace Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.PreAuthPostCheck;

public static class Module
{
    public static IServiceCollection AddPreAuthPostCheck(this IServiceCollection services)
    {
        return services.AddTransient<PreAuthPostCheckStep>();
    }
}