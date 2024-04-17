//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using MultiFactor.Radius.Adapter.Configuration;
using System.Threading.Tasks;

namespace MultiFactor.Radius.Adapter.Core.Ldap;

public interface IUserGroupsGetter
{
    AuthenticationSource AuthenticationSource { get; }
    Task<string[]> GetAllUserGroupsAsync(ILdapConnectionAdapter adapter, string userDn, bool loadNestedGroup);
}