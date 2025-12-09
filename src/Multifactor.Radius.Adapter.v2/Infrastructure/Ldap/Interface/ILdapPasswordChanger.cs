using Multifactor.Radius.Adapter.v2.Domain.Ldap.Interfaces;
using Multifactor.Radius.Adapter.v2.Infrastructure.Ldap.Dto;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Ldap.Interface;

public interface ILdapPasswordChanger
{
    Task<PasswordChangeResponse> ChangeUserPasswordAsync(string newPassword, ILdapProfile context);
}