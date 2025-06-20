using Multifactor.Core.Ldap.Connection;
using Multifactor.Core.Ldap.Connection.LdapConnectionFactory;

namespace Multifactor.Radius.Adapter.v2.Core.Ldap;

public class CustomLdapConnectionFactory : ILdapConnectionFactory
{
    private LdapConnectionFactory _factory;
    
    public CustomLdapConnectionFactory()
    {
        _factory = LdapConnectionFactory.Create();
    }

    public ILdapConnection CreateConnection(LdapConnectionOptions ldapConnectionOptions)
    {
        return _factory.CreateConnection(ldapConnectionOptions);
    }
}