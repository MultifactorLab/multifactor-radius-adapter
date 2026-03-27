using Multifactor.Core.Ldap.Name;

namespace Multifactor.Radius.Adapter.v2.Application.Core.Models.Abstractions;

public interface ILdapServerConfiguration
{
    public string ConnectionString { get; }
    public string Username { get; }
    public string Password { get; }
    public int BindTimeoutSeconds{ get; }
    public IReadOnlyList<DistinguishedName> AccessGroups { get; }
    public IReadOnlyList<DistinguishedName> SecondFaGroups { get; }
    public IReadOnlyList<DistinguishedName> SecondFaBypassGroups { get; }
    public bool LoadNestedGroups { get; }
    public IReadOnlyList<DistinguishedName> NestedGroupsBaseDns { get; }
    public IReadOnlyList<DistinguishedName> AuthenticationCacheGroups { get; }
    public IReadOnlyList<string> PhoneAttributes { get; }
    public string IdentityAttribute { get; }
    public bool RequiresUpn { get; }
    public bool EnableTrustedDomains { get; }
    public bool AlternativeSuffixesEnabled { get; }
    public IReadOnlyList<string> IncludedDomains { get; }
    public IReadOnlyList<string> ExcludedDomains { get; }
    public IReadOnlyList<string> IncludedSuffixes { get; }
    public IReadOnlyList<string> ExcludedSuffixes { get; }
    public IReadOnlyList<string> BypassSecondFactorWhenApiUnreachableGroups { get; }
}