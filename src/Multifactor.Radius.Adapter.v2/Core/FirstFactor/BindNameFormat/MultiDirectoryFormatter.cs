using Multifactor.Core.Ldap.Schema;
using Multifactor.Radius.Adapter.v2.Core.Ldap;
using Multifactor.Radius.Adapter.v2.Core.Ldap.Identity;

namespace Multifactor.Radius.Adapter.v2.Core.FirstFactor.BindNameFormat;

public class MultiDirectoryFormatter : ILdapBindNameFormatter
{
    public LdapImplementation LdapImplementation => LdapImplementation.MultiDirectory;
    
    public string FormatName(string userName, ILdapProfile ldapProfile)
    {
        var identity = new UserIdentity(userName);
        
        if (identity.Format == UserIdentityFormat.UserPrincipalName 
            || identity.Format == UserIdentityFormat.DistinguishedName)
            return userName;

        return ldapProfile.Dn.StringRepresentation;
    }
}