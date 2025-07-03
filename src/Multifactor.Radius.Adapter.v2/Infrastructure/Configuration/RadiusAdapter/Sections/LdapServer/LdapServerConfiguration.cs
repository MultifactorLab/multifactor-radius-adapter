using System.ComponentModel;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Configuration.RadiusAdapter.Sections.LdapServer;

public class LdapServerConfiguration
{
    [Description("connection-string")]
    public string ConnectionString { get; init; } = string.Empty;

    [Description("username")]
    public string UserName { get; init; } = string.Empty;

    [Description("password")]
    public string Password { get; init; } = string.Empty;

    [Description("bind-timeout-in-seconds")]
    public int BindTimeoutInSeconds { get; init; } = 30;

    [Description("access-groups")]
    public string AccessGroups { get; init; } = string.Empty;

    [Description("second-fa-groups")]
    public string SecondFaGroups { get; init; } = string.Empty;

    [Description("second-fa-bypass-groups")]
    public string SecondFaBypassGroups { get; init; } = string.Empty;

    [Description("load-nested-groups")]
    public bool LoadNestedGroups { get; init; } = true;

    [Description("nested-groups-base-dn")]
    public string NestedGroupsBaseDn { get; init; } = string.Empty;

    [Description("phone-attributes")]
    public string PhoneAttributes { get; init; } = string.Empty;

    [Description("identity-attribute")]
    public string IdentityAttribute { get; init; } = string.Empty;
}