using Multifactor.Core.Ldap.Connection;

namespace Multifactor.Radius.Adapter.v2.Core.Ldap;

public interface ILdapConnectionFactory
{
    ILdapConnection CreateConnection(LdapConnectionOptions ldapConnectionOptions);
}