namespace Multifactor.Radius.Adapter.v2.Services.Ldap;

public interface ILdapGroupService
{
    IReadOnlyList<string> LoadUserGroups(LoadUserGroupsRequest request);

    bool IsMemberOf(MembershipRequest request);
}