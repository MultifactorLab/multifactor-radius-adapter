using Microsoft.Extensions.Configuration;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Configuration.RadiusAdapter.Sections.LdapServer;

public class LdapServerConfiguration
{
    public string? ConnectionString { get; init; }
    
    public string? UserName { get; init; }

    public string? Password { get; init; }

    public int BindTimeoutInSeconds { get; init; } = 30;

    public string? AccessGroups { get; init; }

    public string? SecondFaGroups { get; init; }

    public string? SecondFaBypassGroups { get; init; }

    public bool LoadNestedGroups { get; init; }

    public string? NestedGroupsBaseDn { get; init; }

    public string? PhoneAttributes { get; init; }

    public string? IdentityAttribute { get; init; }
}