//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md


//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using MultiFactor.Radius.Adapter.Infrastructure.Configuration.Models;

namespace MultiFactor.Radius.Adapter.Infrastructure.Configuration;

/// <summary>
/// Provides a main (root) Radius Adapter configuration.
/// </summary>
public interface IRootConfigurationProvider
{
    /// <summary>
    /// Returns root configuration.
    /// </summary>
    /// <returns>Radius Adapter Configuration</returns>
    RadiusAdapterConfiguration GetRootConfiguration();
}