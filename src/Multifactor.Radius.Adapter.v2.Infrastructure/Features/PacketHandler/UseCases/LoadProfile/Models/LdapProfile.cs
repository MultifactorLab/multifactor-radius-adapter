using Multifactor.Core.Ldap.Attributes;
using Multifactor.Core.Ldap.Entry;
using Multifactor.Core.Ldap.Name;
using Multifactor.Core.Ldap.Schema;
using Multifactor.Radius.Adapter.v2.Application.Core.Models.Abstractions;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Features.PacketHandler.UseCases.LoadProfile.Models;

public sealed class LdapProfile : ILdapProfile
{
    public LdapProfile(LdapEntry ldapEntry, ILdapSchema? schema = null)
    {
        ArgumentNullException.ThrowIfNull(ldapEntry, nameof(ldapEntry));

        MemberOf = ParseMemberOf(ldapEntry.Attributes["memberOf"]?.GetNotEmptyValues(), schema);
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

    // memberOf in FreeIPA/ALD Pro also contains non-group entries (e.g. sudo/HBAC rules
    // "ipaUniqueID=...,cn=sudorules,cn=sudo,...") that DistinguishedName cannot parse.
    // Skip them instead of failing the whole profile load.
    private static List<DistinguishedName> ParseMemberOf(IEnumerable<string>? values, ILdapSchema? schema)
    {
        var result = new List<DistinguishedName>();
        if (values is null)
            return result;

        foreach (var value in values)
        {
            try
            {
                result.Add(new DistinguishedName(value, schema));
            }
            catch (ArgumentException)
            {
                // not a parseable group DN, irrelevant for membership checks
            }
        }

        return result;
    }
}
