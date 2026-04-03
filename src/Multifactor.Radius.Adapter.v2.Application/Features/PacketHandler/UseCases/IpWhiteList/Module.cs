using Microsoft.Extensions.DependencyInjection;

namespace Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.IpWhiteList;

public static class Module
{
    public static IServiceCollection AddIpWhiteList(this IServiceCollection services)
    {
        return services.AddTransient<IpWhiteListStep>();
    }
}