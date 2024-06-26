﻿//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using MultiFactor.Radius.Adapter.Configuration.Core;
using MultiFactor.Radius.Adapter.Core.Ldap;
using System;
using System.Threading.Tasks;

namespace MultiFactor.Radius.Adapter.Services.Ldap.UserGroupsReading
{
    public class UserGroupsSource
    {
        private readonly UserGroupsGetterProvider _provider;

        public UserGroupsSource(UserGroupsGetterProvider provider)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        public async Task<string[]> GetUserGroupsAsync(IClientConfiguration clientConfig, ILdapConnectionAdapter connectionAdapter, string userDn)
        {
            var getter = _provider.GetUserGroupsGetter(clientConfig.FirstFactorAuthenticationSource);
            return await getter.GetAllUserGroupsAsync(connectionAdapter, userDn, clientConfig.LoadActiveDirectoryNestedGroups);
        }
    }
}