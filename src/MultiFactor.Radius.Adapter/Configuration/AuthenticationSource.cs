//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using System;

namespace MultiFactor.Radius.Adapter.Configuration
{
    [Flags]
    public enum AuthenticationSource
    {
        None = 0,
        ActiveDirectory = 1,
        Radius = 2,
        Ldap = 4
    }
}
