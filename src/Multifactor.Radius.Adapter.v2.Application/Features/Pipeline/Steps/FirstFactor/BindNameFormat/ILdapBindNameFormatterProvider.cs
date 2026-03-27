using Multifactor.Core.Ldap.Schema;

namespace Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.Steps.FirstFactor.BindNameFormat;

public interface ILdapBindNameFormatterProvider
{
    ILdapBindNameFormatter? GetLdapBindNameFormatter(LdapImplementation ldapImplementation);
}