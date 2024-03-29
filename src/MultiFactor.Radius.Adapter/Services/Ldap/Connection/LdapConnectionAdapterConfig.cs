﻿using Microsoft.Extensions.Logging;
using MultiFactor.Radius.Adapter.Core.Ldap;
using MultiFactor.Radius.Adapter.Services.BindIdentityFormatting;

namespace MultiFactor.Radius.Adapter.Services.Ldap.Connection
{
    public class LdapConnectionAdapterConfig
    {
        public IBindIdentityFormatter BindIdentityFormatter { get; set; } = new DefaultBindIdentityFormatter();
        public ILogger Logger { get; set; }
    }
}