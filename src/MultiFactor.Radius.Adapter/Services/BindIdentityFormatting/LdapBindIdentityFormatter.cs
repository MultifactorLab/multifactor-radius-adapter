//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using MultiFactor.Radius.Adapter.Configuration;
using MultiFactor.Radius.Adapter.Configuration.Core;
using MultiFactor.Radius.Adapter.Core.Ldap;
using MultiFactor.Radius.Adapter.Core.Services.Ldap;
using MultiFactor.Radius.Adapter.Services.Ldap;
using System;

namespace MultiFactor.Radius.Adapter.Services.BindIdentityFormatting
{
    public class LdapBindIdentityFormatter : IBindIdentityFormatter
    {
        private readonly IClientConfiguration _clientConfig;

        public LdapBindIdentityFormatter(IClientConfiguration clientConfig)
        {
            _clientConfig = clientConfig ?? throw new ArgumentNullException(nameof(clientConfig));
        }

        public string FormatIdentity(LdapIdentity user, string ldapUri)
        {
            if (user.Type == IdentityType.UserPrincipalName)
            {
                return user.Name;
            }

            var bindDn = $"uid={user.Name}";
            if (!string.IsNullOrEmpty(_clientConfig.LdapBindDn))
            {
                bindDn += "," + _clientConfig.LdapBindDn;
            }

            return bindDn;
        }
    }
}