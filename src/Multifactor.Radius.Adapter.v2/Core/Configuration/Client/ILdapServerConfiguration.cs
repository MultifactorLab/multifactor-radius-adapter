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
    IReadOnlyList<string> AccessGroups { get; }
    IReadOnlyList<string> SecondFaGroups { get; }
    IReadOnlyList<string> SecondFaBypassGroups { get; }
    IReadOnlyList<string> NestedGroupsBaseDns { get; }
    IReadOnlyList<string> PhoneAttributes { get; }
    IDomainPermissionRules? DomainPermissionRules { get; }
    IReadOnlyList<IPAddressRange> IpWhiteList { get; }
    IReadOnlyList<string> AuthenticationCacheGroups { get; }
    int LdapSchemaCacheLifeTimeInHours { get; }
    int UserProfileCacheLifeTimeInHours { get; }
}