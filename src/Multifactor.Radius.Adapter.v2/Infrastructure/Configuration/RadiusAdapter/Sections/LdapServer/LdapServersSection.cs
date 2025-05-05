using System.ComponentModel;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Configuration.RadiusAdapter.Sections.LdapServer;

[Description("LdapServers")]
public class LdapServersSection
{
    [Description("LdapServer")]
    public LdapServerConfiguration[] LdapServer { get; set; }
}