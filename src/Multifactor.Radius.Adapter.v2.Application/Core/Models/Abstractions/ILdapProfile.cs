using Multifactor.Core.Ldap.Attributes;
using Multifactor.Core.Ldap.Name;

namespace Multifactor.Radius.Adapter.v2.Application.Core.Models.Abstractions;

public interface ILdapProfile
{
    DistinguishedName Dn { get; }
    IReadOnlyCollection<DistinguishedName> MemberOf { get; }
    IReadOnlyCollection<LdapAttribute> Attributes { get; }
    string? Phone { get; }
    string? Email { get; }
    string? DisplayName { get; }
}