using Multifactor.Core.Ldap.Schema;

namespace Multifactor.Radius.Adapter.v2.Core.FirstFactor.BindNameFormat;

public interface ILdapBindNameFormatterProvider
{
    ILdapBindNameFormatter? GetLdapBindNameFormatter(LdapImplementation ldapImplementation);
}