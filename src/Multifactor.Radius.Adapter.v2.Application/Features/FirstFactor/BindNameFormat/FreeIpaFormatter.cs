using Multifactor.Core.Ldap.Schema;
using Multifactor.Radius.Adapter.v2.Application.Features.Ldap.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.Models.Enum;
using Multifactor.Radius.Adapter.v2.Application.Models;

namespace Multifactor.Radius.Adapter.v2.Application.Features.FirstFactor.BindNameFormat;

public class FreeIpaFormatter : ILdapBindNameFormatter
{
    public LdapImplementation LdapImplementation => LdapImplementation.FreeIPA;
    
    public string FormatName(string userName, ILdapProfile ldapProfile)
    {
        var identity = new UserIdentity(userName);
        
        if (identity.Format == UserIdentityFormat.UserPrincipalName 
            || identity.Format == UserIdentityFormat.DistinguishedName)
            return userName;

        return ldapProfile.Dn.StringRepresentation;
    }
}