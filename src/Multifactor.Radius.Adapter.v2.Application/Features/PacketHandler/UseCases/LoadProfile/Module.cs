using Microsoft.Extensions.DependencyInjection;

namespace Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.LoadProfile;

public static class Module
{
    public static IServiceCollection AddProfileLoading(this IServiceCollection services)
    {
        return services.AddTransient<ProfileLoadingStep>();
    }
}