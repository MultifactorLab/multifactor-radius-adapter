﻿//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using MultiFactor.Radius.Adapter.Configuration;
using MultiFactor.Radius.Adapter.Configuration.Core;
using MultiFactor.Radius.Adapter.Core.Ldap;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MultiFactor.Radius.Adapter.Services.BindIdentityFormatting
{
    public class BindIdentityFormatterFactory
    {
        private readonly IEnumerable<IBindIdentityFormatter> _formatters;

        public BindIdentityFormatterFactory(IEnumerable<IBindIdentityFormatter> formatters)
        {
            _formatters = formatters ?? throw new ArgumentNullException(nameof(formatters));
        }

        public IBindIdentityFormatter CreateFormatter(IClientConfiguration clientConfiguration)
        {
            switch (clientConfiguration.FirstFactorAuthenticationSource)
            {
                case AuthenticationSource.None:
                case AuthenticationSource.ActiveDirectory:
                    return new ActiveDirectoryBindIdentityFormatter();
                case AuthenticationSource.Ldap:
                    return new LdapBindIdentityFormatter(clientConfiguration);
                default:
                    return new DefaultBindIdentityFormatter();
            }
        }
    }
}