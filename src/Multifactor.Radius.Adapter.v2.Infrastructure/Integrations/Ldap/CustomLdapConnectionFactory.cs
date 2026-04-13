using System.Runtime.InteropServices;
using Multifactor.Core.Ldap.Connection;
using Multifactor.Core.Ldap.Connection.LdapConnectionFactory;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Integrations.Ldap;

internal sealed class CustomLdapConnectionFactory : ILdapConnectionFactory
{
    private readonly LdapConnectionFactory _factory = LdapConnectionFactory.Create();

    public ILdapConnection CreateConnection(LdapConnectionOptions ldapConnectionOptions)
    {
        return new LdapConnection(_factory.CreateConnection(ldapConnectionOptions));
    }

    public OSPlatform TargetPlatform { get; init; }
}