using Microsoft.Extensions.Configuration;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Configuration.RadiusAdapter.Sections.LdapServer;

public class LdapServerConfiguration
{
    [ConfigurationKeyName("connection-string")]
    public string? ConnectionString { get; init; }
    
    [ConfigurationKeyName("username")]
    public string? UserName { get; init; }
    
    [ConfigurationKeyName("password")]
    public string? Password { get; init; }
    
    [ConfigurationKeyName("bind-timeout-in-seconds")]
    public int BindTimeoutInSeconds { get; init; }
    
    [ConfigurationKeyName("access-groups")]
    public string? AccessGroups { get; init; }
    
    [ConfigurationKeyName("second-fa-groups")]
    public string? SecondFaGroups { get; init; }
    
    [ConfigurationKeyName("second-fa-bypass-groups")]
    public string? SecondFaBypassGroups { get; init; }
    
    [ConfigurationKeyName("load-nested-groups")]
    public bool LoadNestedGroups { get; init; }
    
    [ConfigurationKeyName("nested-groups-base-dn")]
    public string? NestedGroupsBaseDn { get; init; }
    
    [ConfigurationKeyName("phone-attributes")]
    public string? PhoneAttributes { get; init; }
    
    [ConfigurationKeyName("identity-attribute")]
    public string? IdentityAttribute { get; init; }
}