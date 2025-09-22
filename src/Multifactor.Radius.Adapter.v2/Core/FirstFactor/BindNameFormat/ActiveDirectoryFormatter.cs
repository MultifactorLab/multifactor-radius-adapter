using Multifactor.Core.Ldap.Schema;
using Multifactor.Radius.Adapter.v2.Core.Ldap;

namespace Multifactor.Radius.Adapter.v2.Core.FirstFactor.BindNameFormat;

public class ActiveDirectoryFormatter : ILdapBindNameFormatter
{
    public LdapImplementation LdapImplementation => LdapImplementation.ActiveDirectory;
    
    public string FormatName(string userName, ILdapProfile ldapProfile)
    {
        return userName;
    }
}