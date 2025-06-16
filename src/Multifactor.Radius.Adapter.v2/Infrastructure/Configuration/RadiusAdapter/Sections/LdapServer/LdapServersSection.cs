using System.ComponentModel;
using Microsoft.Extensions.Configuration;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Configuration.RadiusAdapter.Sections.LdapServer;

[Description("LdapServers")]
public class LdapServersSection
{
    [ConfigurationKeyName("LdapServer")]
    public LdapServerConfiguration?[] LdapServers { get; set; } = [];

    [ConfigurationKeyName("LdapServer")]
    public LdapServerConfiguration? LdapServer { get; set; } = null;

    public LdapServerConfiguration[] Servers
    {
        get
        {
            //because .net always binds empty object instead of null
            if (!string.IsNullOrWhiteSpace(LdapServer?.ConnectionString))
            {
                return [LdapServer];
            }

            var configs = new List<LdapServerConfiguration>();
            foreach (var config in LdapServers)
            {
                if (config != null)
                    configs.Add(config);
            }

            return configs.ToArray();
        }
    }
}