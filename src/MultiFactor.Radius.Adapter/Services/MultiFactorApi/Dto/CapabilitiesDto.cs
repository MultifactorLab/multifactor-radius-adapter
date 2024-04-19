//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

namespace MultiFactor.Radius.Adapter.Services.MultiFactorApi.Dto;

/// <summary>
/// Capabilities of installed radius adapter version
/// </summary>
public class CapabilitiesDto
{
    public bool InlineEnroll { get; set; }
}
