//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using MultiFactor.Radius.Adapter.Configuration;
using MultiFactor.Radius.Adapter.Configuration.Core;
using MultiFactor.Radius.Adapter.Core.Ldap;
using MultiFactor.Radius.Adapter.Services.Ldap.Connection;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MultiFactor.Radius.Adapter.Services.Ldap.UserGroupsReading
{
    public class DefaultUserGroupsGetter : IUserGroupsGetter
    {
        public AuthenticationSource AuthenticationSource => AuthenticationSource.Ldap;

        public Task<IReadOnlyList<string>> GetAllUserGroupsAsync(IClientConfiguration clientConfig, ILdapConnectionAdapter adapter, string userDn)
        {
            return Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());
        }
    }
}