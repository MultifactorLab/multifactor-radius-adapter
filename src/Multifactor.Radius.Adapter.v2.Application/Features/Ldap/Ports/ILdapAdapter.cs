using Multifactor.Core.Ldap.Schema;
using Multifactor.Radius.Adapter.v2.Application.Features.Ldap.Models;

namespace Multifactor.Radius.Adapter.v2.Application.Features.Ldap.Ports;

public interface ILdapAdapter
{
    IReadOnlyList<string> LoadUserGroups(LoadUserGroupRequest request);
    bool IsMemberOf(MembershipRequest request);
    bool ChangeUserPassword(ChangeUserPasswordRequest request);
    bool CheckConnection(LdapConnectionData request);
}