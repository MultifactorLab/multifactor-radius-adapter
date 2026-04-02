using Microsoft.Extensions.DependencyInjection;

namespace Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.LoadLdapForest;

public static class Module
{
    public static IServiceCollection AddLoadLdapForest(this IServiceCollection services)
    {
        return services.AddTransient<LoadLdapForestStep>();
    }
}