using Multifactor.Core.Ldap.Name;

namespace Multifactor.Radius.Adapter.v2.Application.Configuration.Models;

public class LdapServerConfiguration
{
    public string ConnectionString { get; init; }
    public string Username { get; init; }
    public string Password { get; init; }
    public int BindTimeoutSeconds{ get; init; }
    public IReadOnlyList<DistinguishedName> AccessGroups { get; init; }
    public IReadOnlyList<DistinguishedName> SecondFaGroups { get; init; }
    public IReadOnlyList<DistinguishedName> SecondFaBypassGroups { get; init; }
    public bool LoadNestedGroups { get; init; }
    public IReadOnlyList<DistinguishedName> NestedGroupsBaseDns { get; init; }
    public IReadOnlyList<DistinguishedName> AuthenticationCacheGroups { get; init; }
    public IReadOnlyList<string> PhoneAttributes { get; init; }
    public string IdentityAttribute { get; init; }
    public bool RequiresUpn { get; init; }
    public bool TrustedDomainsEnabled { get; init; }
    public bool AlternativeSuffixesEnabled { get; init; }
    public IReadOnlyList<string> IncludedDomains { get; init; }
    public IReadOnlyList<string> ExcludedDomains { get; init; }
    public IReadOnlyList<string> IncludedSuffixes { get; init; }
    public IReadOnlyList<string> ExcludedSuffixes { get; init; }
}