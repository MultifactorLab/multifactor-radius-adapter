//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using MultiFactor.Radius.Adapter.Configuration;
using MultiFactor.Radius.Adapter.Configuration.Core;
using MultiFactor.Radius.Adapter.Core.Ldap;

namespace MultiFactor.Radius.Adapter.Services.BindIdentityFormatting
{
    public class BindIdentityFormatterFactory
    {
        public BindIdentityFormatterFactory() { }

        public IBindIdentityFormatter CreateFormatter(IClientConfiguration clientConfiguration)
        {
            return clientConfiguration.FirstFactorAuthenticationSource switch
            {
                AuthenticationSource.None or AuthenticationSource.ActiveDirectory => new ActiveDirectoryBindIdentityFormatter(),
                AuthenticationSource.Ldap => new LdapBindIdentityFormatter(clientConfiguration.LdapBindDn),
                _ => new DefaultBindIdentityFormatter(),
            };
        }
    }
}