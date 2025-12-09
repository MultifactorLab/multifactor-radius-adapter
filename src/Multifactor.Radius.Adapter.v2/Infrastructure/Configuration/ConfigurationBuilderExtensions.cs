//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using Microsoft.Extensions.Configuration;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configuration.XmlAppConfiguration;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Configuration;

internal static class ConfigurationBuilderExtensions
{
    public const string BasePrefix = "RAD_";

    public static IConfigurationBuilder AddRadiusConfigurationFile(
        this IConfigurationBuilder builder, 
        RadiusConfigurationFile file)
    {
        ArgumentNullException.ThrowIfNull(file);
        builder.Add(new XmlAppConfigurationSource(file));
        return builder;
    }

    public static IConfigurationBuilder AddRadiusEnvironmentVariables(
        this IConfigurationBuilder builder, 
        string? configName = null)
    {
        var preparedName = RadiusConfigurationSource.TransformName(configName);
        var prefix = string.IsNullOrEmpty(preparedName) 
            ? BasePrefix 
            : $"{BasePrefix}{preparedName}_";

        builder.AddEnvironmentVariables(prefix);
        return builder;
    }
}