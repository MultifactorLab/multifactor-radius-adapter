//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using Microsoft.Extensions.Configuration;
using System;

namespace MultiFactor.Radius.Adapter.Configuration.ConfigurationLoading;

internal static class ConfigurationBuilderExtensions
{
    private const string _prefix = "RAD_";

    public static IConfigurationBuilder AddRadiusConfigurationFile(this IConfigurationBuilder configurationBuilder, RadiusConfigurationFile file)
    {
        if (file is null)
        {
            throw new ArgumentNullException(nameof(file));
        }

        configurationBuilder.Add(new XmlAppConfigurationSource(file));
        return configurationBuilder;
    }

    public static IConfigurationBuilder AddRadiusEnvironmentVariables(this IConfigurationBuilder configurationBuilder, string configName = null)
    {
        var prefix = $"{_prefix}{configName}" ?? string.Empty;
        configurationBuilder.AddEnvironmentVariables(prefix);
        return configurationBuilder;
    }
}
