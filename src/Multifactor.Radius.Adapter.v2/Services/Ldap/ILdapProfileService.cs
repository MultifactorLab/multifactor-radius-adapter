using Multifactor.Core.Ldap.Attributes;
using Multifactor.Core.Ldap.Name;
using Multifactor.Radius.Adapter.v2.Core.Ldap;
using Multifactor.Radius.Adapter.v2.Core.Ldap.Identity;

namespace Multifactor.Radius.Adapter.v2.Services.Ldap;

public interface ILdapProfileService
{
    ILdapProfile? LoadLdapProfile(DistinguishedName domain, UserIdentity userIdentity, LdapAttributeName[]? attributeNames = null);
}