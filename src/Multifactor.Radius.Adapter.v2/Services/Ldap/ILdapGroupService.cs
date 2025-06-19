using Multifactor.Core.Ldap.Name;

namespace Multifactor.Radius.Adapter.v2.Services.Ldap;

public interface ILdapGroupService
{
    IReadOnlyList<string> LoadUserGroups(DistinguishedName userName, int limit = int.MaxValue);
}