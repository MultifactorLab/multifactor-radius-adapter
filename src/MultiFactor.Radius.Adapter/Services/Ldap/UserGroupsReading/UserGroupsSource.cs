﻿//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using MultiFactor.Radius.Adapter.Infrastructure.Configuration;
using MultiFactor.Radius.Adapter.Services.Ldap.Connection;
using System;
using System.Threading.Tasks;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration.ClientLevel;

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