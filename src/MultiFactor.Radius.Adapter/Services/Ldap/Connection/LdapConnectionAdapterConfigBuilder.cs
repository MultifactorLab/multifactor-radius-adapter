using MultiFactor.Radius.Adapter.Core.Ldap;
using Serilog;
using System;

namespace MultiFactor.Radius.Adapter.Services.Ldap.Connection
{
    public class LdapConnectionAdapterConfigBuilder
    {
        private readonly LdapConnectionAdapterConfig _config;

        public LdapConnectionAdapterConfigBuilder(LdapConnectionAdapterConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public LdapConnectionAdapterConfigBuilder SetBindIdentityFormatter(IBindIdentityFormatter bindDnFormatter)
        {
            _config.BindIdentityFormatter = bindDnFormatter ?? throw new ArgumentNullException(nameof(bindDnFormatter));
            return this;
        }

        public LdapConnectionAdapterConfigBuilder SetLogger(ILogger logger)
        {
            _config.Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            return this;
        }
    }
}