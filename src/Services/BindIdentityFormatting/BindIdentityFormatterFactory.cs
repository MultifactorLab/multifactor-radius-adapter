//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using MultiFactor.Radius.Adapter.Configuration;
using MultiFactor.Radius.Adapter.Core;
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

        public IBindIdentityFormatter CreateFormatter(ClientConfiguration clientConfiguration)
        {
            switch (clientConfiguration.FirstFactorAuthenticationSource)
            {
                case AuthenticationSource.ActiveDirectory:
                    return new ActiveDirectoryBindIdentityFormatter();
                case AuthenticationSource.Ldap:
                    return new LdapBindIdentityFormatter(clientConfiguration);
                default:
                    throw new NotImplementedException(clientConfiguration.FirstFactorAuthenticationSource.ToString());
            }
        }
    }
}