//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

namespace Multifactor.Radius.Adapter.v2.Core.Auth
{
    [Flags]
    public enum AuthenticationSource
    {
        NotSpecified = 0,
        None = 1,
        Radius = 2,
        Ldap = 4
    }
}
