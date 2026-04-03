using Microsoft.Extensions.DependencyInjection;
using Multifactor.Core.Ldap.Connection.LdapConnectionFactory;
using Multifactor.Core.Ldap.LdapGroup.Load;
using Multifactor.Core.Ldap.LdapGroup.Membership;
using Multifactor.Core.Ldap.Schema;
using Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.FirstFactor.Ports;
using Multifactor.Radius.Adapter.v2.Infrastructure.Integrations.Ldap;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Integrations.Radius;

public static class Module
{
    public static IServiceCollection AddRadiusIntegration(this IServiceCollection services)
    {
        services.AddSingleton<IRadiusClientFactory, RadiusClientFactory>();
        return services;
    }
}