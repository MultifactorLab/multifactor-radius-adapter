//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using MultiFactor.Radius.Adapter.Infrastructure.Configuration;
using MultiFactor.Radius.Adapter.Services.Ldap.Connection;
using System;
using System.Threading.Tasks;

namespace MultiFactor.Radius.Adapter.Services.Ldap.UserGroupsReading
{
    public class DefaultUserGroupsGetter : IUserGroupsGetter
    {
        public AuthenticationSource AuthenticationSource => AuthenticationSource.Ldap;

        public Task<string[]> GetUserGroupsFromContainerAsync(ILdapConnectionAdapter adapter, string baseDn, string userDn, bool loadNestedGroup)
        {
            return Task.FromResult(Array.Empty<string>());
        }
    }
}