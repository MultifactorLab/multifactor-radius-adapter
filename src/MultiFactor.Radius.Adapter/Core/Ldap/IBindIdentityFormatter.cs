//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using MultiFactor.Radius.Adapter.Services.Ldap;

namespace MultiFactor.Radius.Adapter.Core.Ldap;

public interface IBindIdentityFormatter
{
    string FormatIdentity(LdapIdentity user, string ldapUri);
}