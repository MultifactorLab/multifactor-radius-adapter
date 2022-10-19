using MultiFactor.Radius.Adapter.Core;
using MultiFactor.Radius.Adapter.Services.BindIdentityFormatting;
using Serilog;

namespace MultiFactor.Radius.Adapter.Services.Ldap.Connection
{
    public class LdapConnectionAdapterConfig
    {
        public IBindIdentityFormatter BindIdentityFormatter { get; set; } = new DefaultBindIdentityFormatter();
        public ILogger? Logger { get; set; }
    }
}