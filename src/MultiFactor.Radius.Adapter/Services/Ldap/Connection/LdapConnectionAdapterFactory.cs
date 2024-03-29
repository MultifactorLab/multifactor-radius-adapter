﻿//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using Microsoft.Extensions.Logging;
using MultiFactor.Radius.Adapter.Configuration;
using MultiFactor.Radius.Adapter.Configuration.Core;
using MultiFactor.Radius.Adapter.Core.Ldap;
using MultiFactor.Radius.Adapter.Services.BindIdentityFormatting;
using MultiFactor.Radius.Adapter.Services.Ldap.Connection.Exceptions;
using System;
using System.Threading.Tasks;

namespace MultiFactor.Radius.Adapter.Services.Ldap.Connection
{
    public class LdapConnectionAdapterFactory
    {
        private readonly BindIdentityFormatterFactory _bindIdentityFormatterFactory;
        private readonly ILogger _logger;

        public LdapConnectionAdapterFactory(BindIdentityFormatterFactory bindIdentityFormatterFactory, ILogger<LdapConnectionAdapter> logger)
        {
            _bindIdentityFormatterFactory = bindIdentityFormatterFactory ?? throw new ArgumentNullException(nameof(bindIdentityFormatterFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        
        public async Task<ILdapConnectionAdapter> CreateAdapterAsTechnicalAccAsync(string domain, IClientConfiguration clientConfig)
        {
            try
            {
                var user = LdapIdentity.ParseUser(clientConfig.ServiceAccountUser);
                return await LdapConnectionAdapter.CreateAsync(domain, user, clientConfig.ServiceAccountPassword, builder => BuildConfig(builder, clientConfig));
            }
            catch (Exception ex)
            {
                throw new TechnicalAccountErrorException(clientConfig.ServiceAccountUser, domain, ex);
            }
        }

        private void BuildConfig(LdapConnectionAdapterConfigBuilder builder, IClientConfiguration clientConfig)
        {
            builder
                .SetBindIdentityFormatter(_bindIdentityFormatterFactory.CreateFormatter(clientConfig))
                .SetLogger(_logger);
        }
    }
}