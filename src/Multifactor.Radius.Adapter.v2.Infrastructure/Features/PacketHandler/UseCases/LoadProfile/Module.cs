using Microsoft.Extensions.DependencyInjection;
using Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.LoadProfile.Ports;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Features.PacketHandler.UseCases.LoadProfile;

internal static class Module
{
    public static IServiceCollection AddLoadProfileInfra(this IServiceCollection services)
    {
        return services.AddTransient<IProfileSearch, LdapProfileSearch>();
    }
}