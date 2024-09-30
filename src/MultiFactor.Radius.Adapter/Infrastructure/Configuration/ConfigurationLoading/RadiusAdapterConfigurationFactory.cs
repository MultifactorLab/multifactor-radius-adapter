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
    /// Tries to read a configuration from file <paramref name="file"/> and binds it, then returns an instance of <see cref="RadiusAdapterConfiguration"/>. Also adds environment variables if a configuration name <paramref name="name"/> is specified.
    /// </summary>
    /// <param name="file">Configuration file path.</param>
    /// <param name="name">Configuration name.</param>
    /// <returns>Radius Adapter Configuration</returns>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="FileNotFoundException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    public static RadiusAdapterConfiguration Create(RadiusConfigurationFile file, string name = null)
    {
        if (file is null)
        {
            throw new ArgumentNullException(nameof(file));
        }
        
        if (!File.Exists(file))
        {
            throw new FileNotFoundException($"Configuration file '{file}' not found");
        }

        var config = new ConfigurationBuilder()
            .AddRadiusConfigurationFile(file)
            .AddRadiusEnvironmentVariables(name)
            .Build();

        var bounded = config.BindRadiusAdapterConfig();
        if (bounded == null)
        {
            throw new InvalidOperationException($"Fatal: Unable to bind Radius adapter configuration '{file}'");
        }

        return bounded;
    }
    
    /// <summary>
    /// Tries to read a configuration from an environment variables with the specified prefix and binds it, then returns an instance of <see cref="RadiusAdapterConfiguration"/>.
    /// </summary>
    /// <param name="environmentVariable">Instance of <see cref="RadiusConfigurationEnvironmentVariable"/>.</param>
    /// <returns>Radius Adapter Configuration</returns>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    public static RadiusAdapterConfiguration Create(RadiusConfigurationEnvironmentVariable environmentVariable)
    {
        if (environmentVariable is null)
        {
            throw new ArgumentNullException(nameof(environmentVariable));
        }
        
        var config = new ConfigurationBuilder()
            .AddRadiusEnvironmentVariables(environmentVariable.Name)
            .Build();
        
        var bounded = config.BindRadiusAdapterConfig();
        if (bounded == null)
        {
            throw new InvalidOperationException($"Fatal: Unable to bind Radius adapter configuration '{environmentVariable}'");
        }
        
        return bounded;
    }
    
    /// <summary>
    /// Tries to read a common radius configuration from an environment variables and binds it, then returns an instance of <see cref="RadiusAdapterConfiguration"/>.
    /// </summary>
    /// <returns>Radius Adapter Configuration</returns>
    /// <exception cref="InvalidOperationException"></exception>
    public static RadiusAdapterConfiguration Create()
    {
        var config = new ConfigurationBuilder()
            .AddRadiusEnvironmentVariables()
            .Build();
        
        var bounded = config.BindRadiusAdapterConfig();
        if (bounded == null)
        {
            throw new InvalidOperationException("Fatal: Unable to bind Radius adapter root configuration");
        }
        
        return bounded;
    }
}
