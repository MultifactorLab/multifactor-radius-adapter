using Multifactor.Core.Ldap.Name;
using NetTools;

namespace Multifactor.Radius.Adapter.v2.Core.Configuration.Client;

public interface ILdapServerConfiguration
{
    string ConnectionString { get; }
    string UserName { get; }
    string Password { get; }
    int BindTimeoutInSeconds { get; }
    bool LoadNestedGroups { get; }
    string? IdentityAttribute { get; }
    IReadOnlyList<DistinguishedName> AccessGroups { get; }
    IReadOnlyList<DistinguishedName> SecondFaGroups { get; }
    IReadOnlyList<DistinguishedName> SecondFaBypassGroups { get; }
    IReadOnlyList<DistinguishedName> NestedGroupsBaseDns { get; }
    IReadOnlyList<string> PhoneAttributes { get; }
    IReadOnlyList<IPAddressRange> IpWhiteList { get; }
    IReadOnlyList<DistinguishedName> AuthenticationCacheGroups { get; }
    int LdapSchemaCacheLifeTimeInHours { get; }
    int UserProfileCacheLifeTimeInHours { get; }
    IPermissionRules DomainPermissions { get; }
    IPermissionRules SuffixesPermissions { get; }
    bool TrustedDomainsEnabled { get; }
    bool AlternativeSuffixesEnabled { get; }
    bool UpnRequired { get; }
}