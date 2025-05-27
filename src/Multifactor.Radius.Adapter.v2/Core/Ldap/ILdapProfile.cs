using Multifactor.Core.Ldap.Attributes;
using Multifactor.Core.Ldap.Name;

namespace Multifactor.Radius.Adapter.v2.Core.Ldap;

public interface ILdapProfile
{
    public DistinguishedName? Dn { get; }

    public string? Upn { get; }

    public string? Phone { get; }
    
    public string? Email { get; }

    public IReadOnlyCollection<DistinguishedName> MemberOf { get; }
    
    public IReadOnlyCollection<LdapAttribute> Attributes { get; }

    public LdapAttribute? GetAttribute(LdapAttributeName attributeName);
}