//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using LdapForNet;
using MultiFactor.Radius.Adapter.Configuration;
using MultiFactor.Radius.Adapter.Core.Services.Ldap;
using MultiFactor.Radius.Adapter.Services.Ldap;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static LdapForNet.Native.Native;

namespace MultiFactor.Radius.Adapter.Services
{
    /// <summary>
    /// Service to interact with Active Directory
    /// </summary>
    public class ActiveDirectoryService : LdapService
    {
        protected override LdapNames Names => new LdapNames(LdapServerType.ActiveDirectory);

        public ActiveDirectoryService(ServiceConfiguration serviceConfiguration, ILogger logger) : base(serviceConfiguration, logger)
        {
        }

        protected override string FormatBindDn(string ldapUri, LdapIdentity user, ClientConfiguration clientConfig)
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

        protected override async Task LoadAllUserGroups(LdapConnection connection, LdapIdentity domain, LdapProfile profile, ClientConfiguration clientConfig)
        {
            var searchFilter = $"(member:1.2.840.113556.1.4.1941:={profile.DistinguishedNameEscaped})";
            var response = await Query(connection, domain.Name, searchFilter, LdapSearchScope.LDAP_SCOPE_SUB, "DistinguishedName");
            profile.MemberOf = response.Select(entry => entry.Dn).ToList();
        }
    }
}