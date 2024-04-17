//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using MultiFactor.Radius.Adapter.Core.Ldap;
using MultiFactor.Radius.Adapter.Services.Ldap;

namespace MultiFactor.Radius.Adapter.Services.BindIdentityFormatting
{
    public class DefaultBindIdentityFormatter : IBindIdentityFormatter
    {
        public string FormatIdentity(LdapIdentity user, string ldapUri) => user.Name;
    }
}