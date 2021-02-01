//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using LdapForNet;
using MultiFactor.Radius.Adapter.Core.Services.Ldap;
using MultiFactor.Radius.Adapter.Server;
using MultiFactor.Radius.Adapter.Services.Ldap;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using static LdapForNet.Native.Native;

namespace MultiFactor.Radius.Adapter.Services
{
    /// <summary>
    /// Service to interact with Active Directory
    /// </summary>
    public class ActiveDirectoryService : LdapService
    {
        protected override LdapNames Names => new LdapNames(LdapServerType.ActiveDirectory);

        public ActiveDirectoryService(Configuration configuration, ILogger logger) : base(configuration, logger)
        {
        }

        protected override string FormatBindDn(LdapIdentity user)
        {
            if (user.Type == IdentityType.UserPrincipalName)
            {
                return user.Name;
            }

            //try create upn from domain name
            if (Uri.IsWellFormedUriString(_configuration.ActiveDirectoryDomain, UriKind.Absolute))
            {
                var uri = new Uri(_configuration.ActiveDirectoryDomain);
                if (uri.PathAndQuery != null && uri.PathAndQuery != "/")
                {
                    var fqdn = LdapIdentity.DnToFqdn(uri.PathAndQuery);
                    return $"{user.Name}@{fqdn}";
                }
            }

            return user.Name;
        }

        protected override bool IsMemberOf(LdapConnection connection, LdapIdentity domain, LdapIdentity user, LdapProfile profile, string groupName)
        {
            var isValidGroup = IsValidGroup(connection, domain, groupName, out var group);

            if (!isValidGroup)
            {
                _logger.Warning($"Security group '{groupName}' not exists in {domain.Name}");
                return false;
            }

            var searchFilter = $"(&({Names.Identity(user)}={user.Name})(memberOf:1.2.840.113556.1.4.1941:={group.Name}))";
            var response = Query(connection, domain.Name, searchFilter, LdapSearchScope.LDAP_SCOPE_SUB, "DistinguishedName");

            return response.Any();
        }
    }
}