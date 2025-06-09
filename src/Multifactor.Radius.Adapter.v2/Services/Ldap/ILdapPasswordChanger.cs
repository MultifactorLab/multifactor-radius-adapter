using Multifactor.Radius.Adapter.v2.Core.Ldap;

namespace Multifactor.Radius.Adapter.v2.Services.Ldap;

public interface ILdapPasswordChanger
{
    Task<PasswordChangeResponse> ChangeUserPasswordAsync(string newPassword, ILdapProfile context);
}