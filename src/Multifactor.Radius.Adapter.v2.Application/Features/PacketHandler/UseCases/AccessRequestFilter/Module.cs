using Microsoft.Extensions.DependencyInjection;

namespace Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.AccessRequestFilter;

public static class Module
{
    public static IServiceCollection AddAccessRequestFiltering(this IServiceCollection services)
    {
        return services.AddTransient<AccessRequestFilteringStep>();
    }
}