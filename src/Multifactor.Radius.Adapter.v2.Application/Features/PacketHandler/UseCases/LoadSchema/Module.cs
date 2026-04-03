using Microsoft.Extensions.DependencyInjection;

namespace Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.LoadSchema;

public static class Module
{
    public static IServiceCollection AddLoadLdapSchema(this IServiceCollection services)
    {
        return services.AddTransient<LoadLdapSchemaStep>();
    }
}