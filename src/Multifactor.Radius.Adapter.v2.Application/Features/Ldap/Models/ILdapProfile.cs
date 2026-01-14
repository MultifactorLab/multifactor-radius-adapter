using Multifactor.Core.Ldap.Attributes;
using Multifactor.Core.Ldap.Name;

namespace Multifactor.Radius.Adapter.v2.Application.Features.Ldap.Models;

public interface ILdapProfile
{
    DistinguishedName Dn { get; }
    string? Phone { get; }
    string? Email { get; }
    string? DisplayName { get; }
    IReadOnlyCollection<DistinguishedName> MemberOf { get; }
    IReadOnlyCollection<LdapAttribute> Attributes { get; }
}