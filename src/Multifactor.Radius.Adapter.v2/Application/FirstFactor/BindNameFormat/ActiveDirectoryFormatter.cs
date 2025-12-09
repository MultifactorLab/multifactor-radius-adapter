using Multifactor.Core.Ldap.Schema;
using Multifactor.Radius.Adapter.v2.Domain.Ldap.Interfaces;

namespace Multifactor.Radius.Adapter.v2.Application.FirstFactor.BindNameFormat;

public class ActiveDirectoryFormatter : ILdapBindNameFormatter
{
    public LdapImplementation LdapImplementation => LdapImplementation.ActiveDirectory;
    
    public string FormatName(string userName, ILdapProfile ldapProfile)
    {
        return userName;
    }
}