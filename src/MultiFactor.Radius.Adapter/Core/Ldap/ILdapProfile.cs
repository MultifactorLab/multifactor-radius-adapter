using System.Collections.Generic;
using MultiFactor.Radius.Adapter.Services.Ldap;

namespace MultiFactor.Radius.Adapter.Core.Ldap;

public interface ILdapProfile
{
    LdapIdentity BaseDn { get; }
    string DisplayName { get; }
    string DistinguishedName { get; }
    string DistinguishedNameEscaped { get; }
    string Email { get; }
    IReadOnlyDictionary<string, object> LdapAttrs { get; }
    string[] MemberOf { get; }
    string Phone { get; }
    string Upn { get; }
}