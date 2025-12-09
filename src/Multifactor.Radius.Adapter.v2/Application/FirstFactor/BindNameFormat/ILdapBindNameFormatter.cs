using Multifactor.Core.Ldap.Schema;
using Multifactor.Radius.Adapter.v2.Domain.Ldap.Interfaces;

namespace Multifactor.Radius.Adapter.v2.Application.FirstFactor.BindNameFormat;

public interface ILdapBindNameFormatter
{
    LdapImplementation LdapImplementation { get; }
    string FormatName(string userName, ILdapProfile ldapProfile);
}