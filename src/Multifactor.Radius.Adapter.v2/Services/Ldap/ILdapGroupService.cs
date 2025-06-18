using Multifactor.Core.Ldap.Connection;
using Multifactor.Core.Ldap.Name;
using Multifactor.Core.Ldap.Schema;

namespace Multifactor.Radius.Adapter.v2.Services.Ldap;

public interface ILdapGroupService
{
    IReadOnlyList<string> LoadUserGroups(ILdapSchema ldapSchema, ILdapConnection connection, DistinguishedName userName, DistinguishedName? searchBase = null, int limit = int.MaxValue);

    bool IsMemberOf(ILdapSchema ldapSchema, ILdapConnection connection, DistinguishedName userName, DistinguishedName[] groupNames, DistinguishedName? searchBase = null);
}