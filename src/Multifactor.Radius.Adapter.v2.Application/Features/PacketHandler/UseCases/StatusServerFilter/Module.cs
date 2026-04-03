using Microsoft.Extensions.DependencyInjection;

namespace Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.StatusServerFilter;

public static class Module
{
    public static IServiceCollection AddStatusServerFilter(this IServiceCollection services)
    {
        return services.AddTransient<StatusServerFilteringStep>();
    }
}