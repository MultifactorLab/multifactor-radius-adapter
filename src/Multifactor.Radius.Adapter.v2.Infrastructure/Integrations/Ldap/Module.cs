using Microsoft.Extensions.DependencyInjection;
using Multifactor.Core.Ldap.Connection.LdapConnectionFactory;
using Multifactor.Core.Ldap.LdapGroup.Load;
using Multifactor.Core.Ldap.LdapGroup.Membership;
using Multifactor.Core.Ldap.Schema;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Integrations.Ldap;

public static class Module
{
    public static IServiceCollection AddLdapIntegration(this IServiceCollection services)
    {
        services.AddSingleton(LdapConnectionFactory.Create());
        services.AddSingleton<ILdapConnectionFactory, CustomLdapConnectionFactory>(_ => new CustomLdapConnectionFactory());
        services.AddSingleton<ILdapGroupLoaderFactory, LdapGroupLoaderFactory>();
        services.AddSingleton<IMembershipCheckerFactory, MembershipCheckerFactory>();
        services.AddSingleton<LdapSchemaLoader>();
        return services;
    }
}