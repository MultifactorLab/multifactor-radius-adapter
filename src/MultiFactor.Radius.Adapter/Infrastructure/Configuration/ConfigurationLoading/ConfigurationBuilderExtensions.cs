//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using Microsoft.Extensions.Configuration;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration.XmlAppConfiguration;
using System;
using System.Text.RegularExpressions;

namespace MultiFactor.Radius.Adapter.Infrastructure.Configuration.ConfigurationLoading;

internal static class ConfigurationBuilderExtensions
{
    private const string _basePrefix = "RAD_";

    public static IConfigurationBuilder AddRadiusConfigurationFile(this IConfigurationBuilder configurationBuilder, RadiusConfigurationFile file)
    {
        if (file is null)
        {
            throw new ArgumentNullException(nameof(file));
        }

        configurationBuilder.Add(new XmlAppConfigurationSource(file));
        return configurationBuilder;
    }

    public static IConfigurationBuilder AddRadiusEnvironmentVariables(this IConfigurationBuilder configurationBuilder, 
        string configName = null)
    {
        var preparedConfigName = GetName(configName);
        var prefix = preparedConfigName == string.Empty
            ? _basePrefix
            : $"{_basePrefix}{preparedConfigName}_";
        configurationBuilder.AddEnvironmentVariables(prefix);
        return configurationBuilder;
    }

    private static string GetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return string.Empty;
        }
        
        name = Regex.Replace(name, @"\s+", string.Empty);
        return name;
    }
}
