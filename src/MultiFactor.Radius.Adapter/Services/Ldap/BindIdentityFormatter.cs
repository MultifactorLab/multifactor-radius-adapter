//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using MultiFactor.Radius.Adapter.Core.Services.Ldap;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration;
using System;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration.ClientLevel;

namespace MultiFactor.Radius.Adapter.Services.Ldap
{
    public class BindIdentityFormatter
    {
        private readonly IClientConfiguration _clientConfiguration;

        public BindIdentityFormatter(IClientConfiguration clientConfiguration)
        {
            _clientConfiguration = clientConfiguration ?? throw new ArgumentNullException(nameof(clientConfiguration));
        }

        public string FormatIdentity(LdapIdentity user, string ldapUri)
        {
            if (user is null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            if (ldapUri is null)
            {
                throw new ArgumentNullException(nameof(ldapUri));
            }

            var authSource = _clientConfiguration.FirstFactorAuthenticationSource;
            var isFreeIpa = _clientConfiguration.IsFreeIpa && authSource != AuthenticationSource.ActiveDirectory;

            if (isFreeIpa || authSource == AuthenticationSource.Ldap)
            {
                return FormatIdentityLdap(user);
            }

            return authSource switch
            {
                AuthenticationSource.None or AuthenticationSource.ActiveDirectory => FormatIdentityAD(user, ldapUri),
                _ => user.Name,
            };
        }

        private string FormatIdentityAD(LdapIdentity user, string ldapUri)
        {
            if (user.Type == IdentityType.UserPrincipalName)
            {
                return user.Name;
            }

            //try create upn from domain name
            if (Uri.IsWellFormedUriString(ldapUri, UriKind.Absolute))
            {
                var uri = new Uri(ldapUri);
                if (uri.PathAndQuery != null && uri.PathAndQuery != "/")
                {
                    var fqdn = LdapIdentity.DnToFqdn(uri.PathAndQuery);
                    return $"{user.Name}@{fqdn}";
                }
            }

            return user.Name;
        }

        private string FormatIdentityLdap(LdapIdentity user)
        {
            if (user.Type == IdentityType.UserPrincipalName || user.Type == IdentityType.DistinguishedName)
            {
                return user.Name;
            }

            var bindDn = $"uid={user.Name}";
            if (!string.IsNullOrEmpty(_clientConfiguration.LdapBindDn))
            {
                bindDn += $",{_clientConfiguration.LdapBindDn}";
            }

            return bindDn;
        }
    }
}