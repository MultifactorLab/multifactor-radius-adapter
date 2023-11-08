//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using LdapForNet;
using MultiFactor.Radius.Adapter.Configuration;
using MultiFactor.Radius.Adapter.Configuration.Core;
using MultiFactor.Radius.Adapter.Core.Ldap;
using MultiFactor.Radius.Adapter.Core.Services.Ldap;
using MultiFactor.Radius.Adapter.Server;
using MultiFactor.Radius.Adapter.Services.BindIdentityFormatting;
using MultiFactor.Radius.Adapter.Services.Ldap.Connection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static LdapForNet.Native.Native;

namespace MultiFactor.Radius.Adapter.Services.Ldap.UserGroupsReading
{
    public class ActiveDirectoryUserGroupsGetter : IUserGroupsGetter
    {
        public AuthenticationSource AuthenticationSource => AuthenticationSource.ActiveDirectory | AuthenticationSource.None;

        public async Task<string[]> GetAllUserGroupsAsync(IClientConfiguration clientConfig, ILdapConnectionAdapter adapter, string userDn)
        {
            if (!clientConfig.LoadActiveDirectoryNestedGroups)
            {
                return Array.Empty<string>();
            }
            var searchFilter = $"(member:1.2.840.113556.1.4.1941:={EscapeUserDn(userDn)})";
            var domain = await adapter.WhereAmIAsync();
            var response = await adapter.SearchQueryAsync(domain.Name, searchFilter, LdapSearchScope.LDAP_SCOPE_SUB, "DistinguishedName");
            return response.Select(entry => LdapIdentity.DnToCn(entry.Dn)).ToArray();
        }

        public string EscapeUserDn(string userDn)
        {
            var ret = userDn
                .Replace("(", @"\28")
                .Replace(")", @"\29");

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                ret = ret.Replace("\"", "\\\""); // quotes
                ret = ret.Replace("\\,", "\\5C,"); // comma
                ret = ret.Replace("\\=", "\\5C="); // \=
            }

            return ret;
        }
    }
}