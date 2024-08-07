//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using System;
using System.Threading.Tasks;
using MultiFactor.Radius.Adapter.Services.Ldap.Profile;

namespace MultiFactor.Radius.Adapter.Services.Ldap.Connection;

public interface ILdapConnectionAdapter : IDisposable
{
    string Path { get; }

    /// <summary>
    /// Returns user that has been successfully binded with LDAP directory.
    /// </summary>
    LdapIdentity Username { get; }

    Task<LdapDomain> WhereAmIAsync();
    Task<ILdapAttributes[]> SearchQueryAsync(string baseDn, string filter, SearchScope scope, params string[] attributes);
}

public enum SearchScope
{
    DEFAULT = -1, // 0xFFFFFFFF
    BASE = 0,
    ONELEVEL = 1,
    SUBTREE = 2,
    CHILDREN = 3
}