using Multifactor.Radius.Adapter.v2.Core.Ldap;

namespace Multifactor.Radius.Adapter.v2.Services.Ldap;

public interface ILdapProfileService
{
    ILdapProfile? FindUserProfile(FindUserProfileRequest request);
    Task<PasswordChangeResponse> ChangeUserPasswordAsync(ChangeUserPasswordRequest request);
}