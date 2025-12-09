using Multifactor.Core.Ldap.Connection;
using Multifactor.Core.Ldap.Schema;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Ldap.Interface;

public interface ILdapSchemaLoader
{
    ILdapSchema? Load(LdapConnectionOptions connectionOptions);
}