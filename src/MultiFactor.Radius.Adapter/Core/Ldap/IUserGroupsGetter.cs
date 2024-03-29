﻿//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using MultiFactor.Radius.Adapter.Configuration;
using MultiFactor.Radius.Adapter.Configuration.Core;
using MultiFactor.Radius.Adapter.Services.Ldap;
using MultiFactor.Radius.Adapter.Services.Ldap.Connection;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MultiFactor.Radius.Adapter.Core.Ldap;

public interface IUserGroupsGetter
{
    AuthenticationSource AuthenticationSource { get; }
    Task<string[]> GetAllUserGroupsAsync(IClientConfiguration clientConfig, ILdapConnectionAdapter connectionAdapter, string userDn);
}