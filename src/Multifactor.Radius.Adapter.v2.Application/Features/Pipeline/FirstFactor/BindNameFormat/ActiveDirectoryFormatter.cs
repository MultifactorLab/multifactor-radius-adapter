using Multifactor.Core.Ldap.Schema;
using Multifactor.Radius.Adapter.v2.Application.Features.Ldap.Models;

namespace Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.FirstFactor.BindNameFormat;

public class ActiveDirectoryFormatter : ILdapBindNameFormatter
{
    public LdapImplementation LdapImplementation => LdapImplementation.ActiveDirectory;
    
    public string FormatName(string userName, ILdapProfile ldapProfile)
    {
        return userName;
    }
}