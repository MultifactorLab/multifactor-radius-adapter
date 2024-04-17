//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using MultiFactor.Radius.Adapter.Configuration;
using MultiFactor.Radius.Adapter.Configuration.Core;
using MultiFactor.Radius.Adapter.Core.Ldap;
using System;
using System.Threading.Tasks;

namespace MultiFactor.Radius.Adapter.Services.Ldap.UserGroupsReading
{
    public class DefaultUserGroupsGetter : IUserGroupsGetter
    {
        public AuthenticationSource AuthenticationSource => AuthenticationSource.Ldap | AuthenticationSource.Radius;

        public Task<string[]> GetAllUserGroupsAsync(ILdapConnectionAdapter adapter, string userDn, bool loadNestedGroup)
        {
            return Task.FromResult(Array.Empty<string>());
        }
    }
}