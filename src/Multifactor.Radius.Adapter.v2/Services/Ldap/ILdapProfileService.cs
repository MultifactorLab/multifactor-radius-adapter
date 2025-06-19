using Multifactor.Core.Ldap.Attributes;
using Multifactor.Core.Ldap.Name;
using Multifactor.Radius.Adapter.v2.Core.Configuration.Client;
using Multifactor.Radius.Adapter.v2.Core.Ldap;
using Multifactor.Radius.Adapter.v2.Core.Ldap.Identity;

namespace Multifactor.Radius.Adapter.v2.Services.Ldap;

public interface ILdapProfileService
{
    ILdapProfile? FindUserProfile(string clientName, ILdapServerConfiguration serverConfiguration, DistinguishedName searchBase, UserIdentity userIdentity, LdapAttributeName[]? attributeNames = null);

    Task<PasswordChangeResponse> ChangeUserPasswordAsync(string newPassword, ILdapProfile ldapProfile, ILdapServerConfiguration serverConfiguration);
}