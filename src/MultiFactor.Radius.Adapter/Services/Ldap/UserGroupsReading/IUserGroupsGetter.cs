//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using MultiFactor.Radius.Adapter.Infrastructure.Configuration;
using MultiFactor.Radius.Adapter.Services.Ldap.Connection;
using System.Threading.Tasks;

namespace MultiFactor.Radius.Adapter.Services.Ldap.UserGroupsReading;

public interface IUserGroupsGetter
{
    AuthenticationSource AuthenticationSource { get; }
    Task<string[]> GetAllUserGroupsAsync(ILdapConnectionAdapter adapter, string userDn, bool loadNestedGroup);
}