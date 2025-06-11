using System.ComponentModel;
using Microsoft.Extensions.Configuration;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Configuration.RadiusAdapter.Sections.LdapServer;

public class LdapServerConfiguration
{
    [ConfigurationKeyName("connection-string")]
    [Description("connection-string")]
    public string? ConnectionString { get; init; }
    
    [ConfigurationKeyName("username")]
    [Description("username")]
    public string? UserName { get; init; }
    
    [ConfigurationKeyName("password")]
    [Description("password")]
    public string? Password { get; init; }

    [ConfigurationKeyName("bind-timeout-in-seconds")]
    [Description("bind-timeout-in-seconds")]
    public int BindTimeoutInSeconds { get; init; } = 30;
    
    [ConfigurationKeyName("access-groups")]
    [Description("access-groups")]
    public string? AccessGroups { get; init; }
    
    [ConfigurationKeyName("second-fa-groups")]
    [Description("second-fa-groups")]
    public string? SecondFaGroups { get; init; }
    
    [ConfigurationKeyName("second-fa-bypass-groups")]
    [Description("second-fa-bypass-groups")]
    public string? SecondFaBypassGroups { get; init; }
    
    [ConfigurationKeyName("load-nested-groups")]
    [Description("load-nested-groups")]
    public bool LoadNestedGroups { get; init; }
    
    [ConfigurationKeyName("nested-groups-base-dn")]
    [Description("nested-groups-base-dn")]
    public string? NestedGroupsBaseDn { get; init; }
    
    [ConfigurationKeyName("phone-attributes")]
    [Description("phone-attributes")]
    public string? PhoneAttributes { get; init; }
    
    [ConfigurationKeyName("identity-attribute")]
    [Description("identity-attribute")]
    public string? IdentityAttribute { get; init; }
}