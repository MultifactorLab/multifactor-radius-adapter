//Copyright(c) 2022 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md
namespace Multifactor.Radius.Adapter.v2.Application.Core.Enum;

[Flags]
public enum AuthenticationSource
{
    None = 0,
    Radius = 1,
    Ldap = 2
}