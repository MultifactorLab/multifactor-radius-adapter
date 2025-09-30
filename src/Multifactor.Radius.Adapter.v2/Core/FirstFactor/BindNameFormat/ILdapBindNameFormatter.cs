using Multifactor.Core.Ldap.Schema;
using Multifactor.Radius.Adapter.v2.Core.Ldap;

namespace Multifactor.Radius.Adapter.v2.Core.FirstFactor.BindNameFormat;

public interface ILdapBindNameFormatter
{
    LdapImplementation LdapImplementation { get; }
    string FormatName(string userName, ILdapProfile ldapProfile);
}