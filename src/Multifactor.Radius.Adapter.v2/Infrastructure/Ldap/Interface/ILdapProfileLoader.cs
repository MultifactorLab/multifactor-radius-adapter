using System.DirectoryServices.Protocols;
using Multifactor.Core.Ldap.Attributes;
using Multifactor.Radius.Adapter.v2.Domain.Ldap.Interfaces;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Ldap.Interface;

public interface ILdapProfileLoader
{
    public ILdapProfile? LoadLdapProfile(
        string filter,
        SearchScope scope = SearchScope.Subtree,
        params LdapAttributeName[] attributeNames);
}