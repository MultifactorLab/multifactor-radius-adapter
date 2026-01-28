using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging.Abstractions;
using Multifactor.Core.Ldap.Connection;
using Multifactor.Core.Ldap.Connection.LdapConnectionFactory;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Adapters.Ldap;

public class CustomLdapConnectionFactory : ILdapConnectionFactory
{
    private LdapConnectionFactory _factory;
    
    public CustomLdapConnectionFactory()
    {
        _factory = LdapConnectionFactory.Create();
    }

    public ILdapConnection CreateConnection(LdapConnectionOptions ldapConnectionOptions)
    {
        return new LdapConnection(_factory.CreateConnection(ldapConnectionOptions));
    }

    public OSPlatform TargetPlatform { get; }
}