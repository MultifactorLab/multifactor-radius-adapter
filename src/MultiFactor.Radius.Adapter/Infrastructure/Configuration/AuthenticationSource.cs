//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using System;

namespace MultiFactor.Radius.Adapter.Infrastructure.Configuration
{
    [Flags]
    public enum AuthenticationSource
    {
        NotSpecified = 0,
        None = 1,
        ActiveDirectory = 2,
        Radius = 4,
        Ldap = 8
    }
}
