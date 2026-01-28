using Multifactor.Core.Ldap.Name;
using Multifactor.Radius.Adapter.v2.Shared.Attributes;

namespace Multifactor.Radius.Adapter.v2.Application.Configuration.Models;

public class LdapServerConfiguration
{
    [ConfigParameter("connection-string")]
    public string ConnectionString { get; init; }
    [ConfigParameter("username")]
    public string Username { get; init; }
    [ConfigParameter("password")]
    public string Password { get; init; }
    [ConfigParameter("bind-timeout-in-seconds")]
    public int BindTimeoutSeconds{ get; init; }
    [ConfigParameter("access-groups")]
    public IReadOnlyList<DistinguishedName> AccessGroups { get; init; }
    [ConfigParameter("second-fa-groups")]
    public IReadOnlyList<DistinguishedName> SecondFaGroups { get; init; }
    [ConfigParameter("second-fa-bypass-groups")]
    public IReadOnlyList<DistinguishedName> SecondFaBypassGroups { get; init; }
    [ConfigParameter("load-nested-groups")]
    public bool LoadNestedGroups { get; init; }
    [ConfigParameter("nested-groups-base-dn")]
    public IReadOnlyList<DistinguishedName> NestedGroupsBaseDns { get; init; }
    [ConfigParameter("authentication-cache-groups")]
    public IReadOnlyList<DistinguishedName> AuthenticationCacheGroups { get; init; }
    [ConfigParameter("phone-attributes")]
    public IReadOnlyList<string> PhoneAttributes { get; init; }
    [ConfigParameter("identity-attribute")]
    public string IdentityAttribute { get; init; }
    [ConfigParameter("requires-upn")]
    public bool RequiresUpn { get; init; }
    [ConfigParameter("enable-trusted-domains")]
    public bool TrustedDomainsEnabled { get; init; }
    [ConfigParameter("enable-alternative-suffixes")]
    public bool AlternativeSuffixesEnabled { get; init; }
    [ConfigParameter("included-domains")]
    public IReadOnlyList<string> IncludedDomains { get; init; }//TODO not used
    [ConfigParameter("excluded-domains")]
    public IReadOnlyList<string> ExcludedDomains { get; init; }//TODO not used
    [ConfigParameter("included-suffixes")]
    public IReadOnlyList<string> IncludedSuffixes { get; init; }
    [ConfigParameter("excluded-suffixes")]
    public IReadOnlyList<string> ExcludedSuffixes { get; init; }
    [ConfigParameter("bypass-second-factor-when-api-unreachable-groups")]
    public IReadOnlyList<string> BypassSecondFactorWhenApiUnreachableGroups { get; init; }
}