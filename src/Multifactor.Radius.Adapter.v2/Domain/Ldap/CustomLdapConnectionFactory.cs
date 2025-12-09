using Microsoft.Extensions.Logging.Abstractions;
using Multifactor.Core.Ldap.Connection;
using Multifactor.Core.Ldap.Connection.LdapConnectionFactory;
using ILdapConnection = Multifactor.Radius.Adapter.v2.Domain.Ldap.Interfaces.ILdapConnection;
using ILdapConnectionFactory = Multifactor.Radius.Adapter.v2.Domain.Ldap.Interfaces.ILdapConnectionFactory;

namespace Multifactor.Radius.Adapter.v2.Domain.Ldap;

public class CustomLdapConnectionFactory : ILdapConnectionFactory
{
    private LdapConnectionFactory _factory;
    
    public CustomLdapConnectionFactory()
    {
        _factory = LdapConnectionFactory.Create();
    }
    
    public CustomLdapConnectionFactory(IEnumerable<Multifactor.Core.Ldap.Connection.LdapConnectionFactory.ILdapConnectionFactory> ldapConnectionFactories)
    {
        _factory = new LdapConnectionFactory (NullLogger<LdapConnectionFactory>.Instance, ldapConnectionFactories);
    }

    public ILdapConnection CreateConnection(LdapConnectionOptions ldapConnectionOptions)
    {
        return new LdapConnection(_factory.CreateConnection(ldapConnectionOptions));
    }
}