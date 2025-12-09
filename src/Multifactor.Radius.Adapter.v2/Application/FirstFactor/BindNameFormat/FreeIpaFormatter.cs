using Multifactor.Core.Ldap.Schema;
using Multifactor.Radius.Adapter.v2.Domain.Ldap.Identity;
using Multifactor.Radius.Adapter.v2.Domain.Ldap.Interfaces;

namespace Multifactor.Radius.Adapter.v2.Application.FirstFactor.BindNameFormat;

public class FreeIpaFormatter : ILdapBindNameFormatter
{
    public LdapImplementation LdapImplementation => LdapImplementation.FreeIPA;
    
    public string FormatName(string userName, ILdapProfile ldapProfile)
    {
        var identity = new UserIdentity(userName);
        
        return identity.Format is UserIdentityFormat.UserPrincipalName 
            or UserIdentityFormat.DistinguishedName ? userName 
            : ldapProfile.Dn.StringRepresentation;
    }
}