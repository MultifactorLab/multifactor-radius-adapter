using System.DirectoryServices.Protocols;
using Multifactor.Core.Ldap.Attributes;
using Multifactor.Radius.Adapter.v2.Core.Ldap;

namespace Multifactor.Radius.Adapter.v2.Services.Ldap;

public interface ILdapProfileLoader
{
    public ILdapProfile? LoadLdapProfile(
        string filter,
        SearchScope scope = SearchScope.Subtree,
        params LdapAttributeName[] attributeNames);
}