//Copyright(c) 2022 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md
namespace Multifactor.Radius.Adapter.v2.Application.Core.Enum;

[Flags]
public enum PreAuthMode
{
    /// <summary>
    /// No second factor
    /// </summary>
    None = 0,
    /// <summary>
    /// Any second factor
    /// </summary>
    Any = 1,
    /// <summary>
    /// One-time password
    /// </summary>
    Otp = 2
}