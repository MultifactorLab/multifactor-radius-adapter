using Microsoft.Extensions.DependencyInjection;
using Multifactor.Radius.Adapter.v2.Infrastructure.Integrations.Ldap;
using Multifactor.Radius.Adapter.v2.Infrastructure.Integrations.Multifactor;
using Multifactor.Radius.Adapter.v2.Infrastructure.Integrations.Radius;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Integrations;

public static class Module
{
    public static IServiceCollection AddIntegrations(this IServiceCollection services)
    {
        services.AddLdapIntegration();
        services.AddMultifactorApi();
        services.AddRadiusIntegration();
        return services;
    }
}