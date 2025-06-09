using System.ComponentModel;
using Microsoft.Extensions.Configuration;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Configuration.RadiusAdapter.Sections.LdapServer;

[Description("LdapServers")]
public class LdapServersSection
{
    [ConfigurationKeyName("LdapServer")]
    public LdapServerConfiguration[]? LdapServers { get; set; }
    
    [ConfigurationKeyName("LdapServer")]
    public LdapServerConfiguration? LdapServer { get; set; }

    public LdapServerConfiguration[] Servers
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(LdapServer?.ConnectionString))
            {
                return new[] { LdapServer };
            }
            
            if (LdapServers != null && LdapServers.All(x => !string.IsNullOrWhiteSpace(x.ConnectionString)))
            {
                return LdapServers;
            }

            return Array.Empty<LdapServerConfiguration>();
        }
    }
}