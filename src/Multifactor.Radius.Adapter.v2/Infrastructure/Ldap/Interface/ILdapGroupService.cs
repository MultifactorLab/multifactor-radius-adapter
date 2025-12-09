using Multifactor.Radius.Adapter.v2.Infrastructure.Ldap.Dto;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Ldap.Interface;

public interface ILdapGroupService
{
    IReadOnlyList<string> LoadUserGroups(LoadUserGroupsRequest request);

    bool IsMemberOf(MembershipRequest request);
}