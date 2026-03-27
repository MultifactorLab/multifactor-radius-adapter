using Multifactor.Core.Ldap.Attributes;
using Multifactor.Core.Ldap.Entry;
using Multifactor.Core.Ldap.Name;
using Multifactor.Core.Ldap.Schema;
using Multifactor.Radius.Adapter.v2.Application.Core.Models.Abstractions;

namespace Multifactor.Radius.Adapter.v2.Application.Core.Models;

public sealed class LdapProfile : ILdapProfile
{
    public LdapProfile(LdapEntry ldapEntry, ILdapSchema? schema = null)
    {
        ArgumentNullException.ThrowIfNull(ldapEntry, nameof(ldapEntry));

        MemberOf = ldapEntry.Attributes["memberOf"]?.GetNotEmptyValues().Select(n => new DistinguishedName(n, schema)).ToList() ?? [];
        Dn = ldapEntry.Dn;
        Phone = ldapEntry.Attributes["mobile"]?.GetNotEmptyValues().FirstOrDefault() ?? ldapEntry.Attributes["phone"]?.GetNotEmptyValues().FirstOrDefault();
        Email = ldapEntry.Attributes["mail"]?.GetNotEmptyValues().FirstOrDefault() ?? ldapEntry.Attributes["email"]?.GetNotEmptyValues().FirstOrDefault();
        DisplayName = ldapEntry.Attributes["displayName"]?.GetNotEmptyValues().FirstOrDefault();
        Attributes = ldapEntry.Attributes?.ToList() ?? [];
    }
    
    public DistinguishedName Dn { get; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? DisplayName { get; set; }
    public IReadOnlyCollection<DistinguishedName> MemberOf { get; }
    public IReadOnlyCollection<LdapAttribute> Attributes { get; }
}