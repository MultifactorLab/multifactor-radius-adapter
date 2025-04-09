//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using MultiFactor.Radius.Adapter.Infrastructure.Configuration;
using MultiFactor.Radius.Adapter.Services.Ldap.Connection;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using static LdapForNet.Native.Native;

namespace MultiFactor.Radius.Adapter.Services.Ldap.UserGroupsReading
{
    public class ActiveDirectoryUserGroupsGetter : IUserGroupsGetter
    {
        public AuthenticationSource AuthenticationSource => AuthenticationSource.ActiveDirectory | AuthenticationSource.None | AuthenticationSource.Radius;

        public async Task<string[]> GetUserGroupsFromContainerAsync(ILdapConnectionAdapter adapter, string baseDn, string userDn, bool loadNestedGroup)
        {
            if (!loadNestedGroup)
            {
                return Array.Empty<string>();
            }
            
            var searchFilter = $"(&(objectCategory=group)(member:1.2.840.113556.1.4.1941:={EscapeUserDn(userDn)}))";
            var response = await adapter.SearchQueryAsync(baseDn, searchFilter, LdapSearchScope.LDAP_SCOPE_SUB, "DistinguishedName");
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