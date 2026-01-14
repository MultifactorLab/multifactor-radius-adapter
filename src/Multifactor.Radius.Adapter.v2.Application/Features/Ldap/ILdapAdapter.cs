using Multifactor.Core.Ldap.Connection;
using Multifactor.Core.Ldap.Name;
using Multifactor.Core.Ldap.Schema;
using Multifactor.Radius.Adapter.v2.Application.Features.Ldap.Models;
using Multifactor.Radius.Adapter.v2.Application.Models;

namespace Multifactor.Radius.Adapter.v2.Application.Features.Ldap;

public interface ILdapAdapter
{
    IReadOnlyList<string> LoadUserGroups(LoadUserGroupRequest request);
    bool IsMemberOf(MembershipRequest request);
    ILdapProfile? FindUserProfile(FindUserRequest request);
    bool ChangeUserPassword(ChangeUserPasswordRequest request);
    ILdapSchema? LoadSchema(LoadSchemaRequest request);
    bool CheckConnecion(CheckConnectionRequest request);
}