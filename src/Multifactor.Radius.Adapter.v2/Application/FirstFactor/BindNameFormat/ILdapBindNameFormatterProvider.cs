using Multifactor.Core.Ldap.Schema;

namespace Multifactor.Radius.Adapter.v2.Application.FirstFactor.BindNameFormat;

public interface ILdapBindNameFormatterProvider
{
    ILdapBindNameFormatter? GetLdapBindNameFormatter(LdapImplementation ldapImplementation);
}