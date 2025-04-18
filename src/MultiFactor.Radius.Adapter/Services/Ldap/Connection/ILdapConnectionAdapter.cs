﻿//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using LdapForNet;
using MultiFactor.Radius.Adapter.Services.Ldap;
using System;
using System.Threading.Tasks;
using static LdapForNet.Native.Native;

namespace MultiFactor.Radius.Adapter.Services.Ldap.Connection;

public interface ILdapConnectionAdapter : IDisposable
{
    string Uri { get; }

    /// <summary>
    /// Returns user that has been successfully binded with LDAP directory.
    /// </summary>
    LdapIdentity BindedUser { get; }

    Task<LdapDomain> WhereAmIAsync();
    Task<LdapEntry[]> SearchQueryAsync(string baseDn, string filter, LdapSearchScope scope, params string[] attributes);
    Task BindAsync(string bindDn, string password);
    Task<DirectoryResponse> SendRequestAsync(DirectoryRequest request);
}