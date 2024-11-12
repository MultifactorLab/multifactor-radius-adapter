//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using MultiFactor.Radius.Adapter.Infrastructure.Configuration.Models;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration.XmlAppConfiguration;

namespace MultiFactor.Radius.Adapter.Infrastructure.Configuration;

/// <summary>
/// Provides Radius Adapter client configurations (in multi-client mode).
/// </summary>
public interface IClientConfigurationsProvider
{
    /// <summary>
    /// Returns a config descriptor from which the specified configuration was read.
    /// </summary>
    /// <param name="configuration">Configuration instance.</param>
    /// <returns>Radius Configuration File</returns>
    RadiusConfigurationSource GetSource(RadiusAdapterConfiguration configuration);

    /// <summary>
    /// Returns all client configurations.
    /// </summary>
    /// <returns></returns>
    RadiusAdapterConfiguration[] GetClientConfigurations();
}