using Multifactor.Radius.Adapter.v2.Domain.Ldap.Interfaces;
using Multifactor.Radius.Adapter.v2.Infrastructure.Ldap.Dto;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Ldap.Interface;

public interface ILdapProfileService
{
    ILdapProfile? FindUserProfile(FindUserProfileRequest request);
    Task<PasswordChangeResponse> ChangeUserPasswordAsync(ChangeUserPasswordRequest request);
}