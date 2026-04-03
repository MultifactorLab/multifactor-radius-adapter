using Microsoft.Extensions.DependencyInjection;
using Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.LoadSchema.Ports;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Features.PacketHandler.UseCases.LoadSchema;

public static class Module
{
    public static IServiceCollection AddLoadSchemaInfra(this IServiceCollection services)
    {
        services.AddSingleton<ISchemaCache, SchemaCache>();
        services.AddTransient<ILoadLdapSchema, LoadLdapSchema>();
        return services;
    }
}