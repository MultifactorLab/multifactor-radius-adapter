using Multifactor.Core.Ldap.Connection;

namespace Multifactor.Radius.Adapter.v2.Domain.Ldap.Interfaces;

public interface ILdapConnectionFactory
{
    ILdapConnection CreateConnection(LdapConnectionOptions ldapConnectionOptions);
}