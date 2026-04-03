using Microsoft.Extensions.DependencyInjection;

namespace Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.PreAuthCheck;

public static class Module
{
    public static IServiceCollection AddPreAuthCheck(this IServiceCollection services)
    {
        return services.AddTransient<PreAuthCheckStep>();
    }
}