using Multifactor.Core.Ldap.Attributes;
using Multifactor.Core.Ldap.Entry;
using Multifactor.Core.Ldap.LangFeatures;
using Multifactor.Core.Ldap.Name;
using Multifactor.Core.Ldap.Schema;

namespace Multifactor.Radius.Adapter.v2.Core.Ldap;

public class LdapProfile : ILdapProfile
{
    private readonly LdapEntry _ldapEntry;

    public LdapProfile(LdapEntry ldapEntry, ILdapSchema? schema = null)
    {
        Throw.IfNull(ldapEntry, nameof(ldapEntry));
        _ldapEntry = ldapEntry;

        MemberOf = _ldapEntry.Attributes["memberOf"]?.GetNotEmptyValues().Select(n => new DistinguishedName(n, schema)).ToList() ?? [];
        Dn = ldapEntry.Dn;
        Upn = _ldapEntry.Attributes["userPrincipalName"]?.GetNotEmptyValues().FirstOrDefault();
        Phone = _ldapEntry.Attributes["phone"]?.GetNotEmptyValues().FirstOrDefault();
        Email = _ldapEntry.Attributes["mail"]?.GetNotEmptyValues().FirstOrDefault() ?? _ldapEntry.Attributes["email"]?.GetNotEmptyValues().FirstOrDefault();
        DisplayName = _ldapEntry.Attributes["displayName"]?.GetNotEmptyValues().FirstOrDefault();
        Attributes = _ldapEntry.Attributes?.ToList() ?? [];
    }
    
    public DistinguishedName Dn { get; }
    public string? Upn { get; }
    public string? Phone { get; }
    public string? Email { get; }
    public string? DisplayName { get; }
    public IReadOnlyCollection<DistinguishedName> MemberOf { get; }
    public IReadOnlyCollection<LdapAttribute> Attributes { get; }
}