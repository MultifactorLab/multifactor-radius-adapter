using Multifactor.Core.Ldap.Schema;
using Multifactor.Radius.Adapter.v2.Application.Core.Models.Abstractions;

namespace Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.Steps.FirstFactor.BindNameFormat;

public interface ILdapBindNameFormatter
{
    LdapImplementation LdapImplementation { get; }
    string FormatName(string userName, ILdapProfile ldapProfile);
}