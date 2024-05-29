//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using Microsoft.Extensions.Configuration;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration.XmlAppConfiguration;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration.Models;
using System;
using System.IO;

namespace MultiFactor.Radius.Adapter.Infrastructure.Configuration.ConfigurationLoading;

/// <summary>
/// Creates instance of <see cref="RadiusAdapterConfiguration"/>.
/// </summary>
internal static class RadiusAdapterConfigurationFactory
{
    /// <summary>
    /// Tries to read the configuration from file <paramref name="path"/> and binds it, then returns an instance of <see cref="RadiusAdapterConfiguration"/>. Also adds environment variables if a configuration name <paramref name="name"/> is specified.
    /// </summary>
    /// <param name="path">Configuration file path.</param>
    /// <param name="name">Configuration name.</param>
    /// <returns>Radius Adapter Configuration</returns>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="FileNotFoundException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    public static RadiusAdapterConfiguration Create(RadiusConfigurationFile path, string name = null)
    {
        if (path is null)
        {
            throw new ArgumentNullException(nameof(path));
        }

        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Configuration file '{path}' not found");
        }

        var config = new ConfigurationBuilder()
            .AddRadiusConfigurationFile(path)
            .AddRadiusEnvironmentVariables(name)
            .Build();

        var bounded = config.BindRadiusAdapterConfig();
        if (bounded == null)
        {
            throw new InvalidOperationException($"Fatal: Unable to bind Radius adapter configuration '{path}'");
        }

        return bounded;
    }
}
