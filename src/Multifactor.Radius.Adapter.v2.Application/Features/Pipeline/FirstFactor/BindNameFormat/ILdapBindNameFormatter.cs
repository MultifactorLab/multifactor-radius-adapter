using Multifactor.Core.Ldap.Schema;
using Multifactor.Radius.Adapter.v2.Application.Features.Ldap.Models;

namespace Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.FirstFactor.BindNameFormat;

public interface ILdapBindNameFormatter
{
    LdapImplementation LdapImplementation { get; }
    string FormatName(string userName, ILdapProfile ldapProfile);
}