//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using MultiFactor.Radius.Adapter.Infrastructure.Configuration.XmlAppConfiguration;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration.Models;
using System;
using System.Reflection;

namespace MultiFactor.Radius.Adapter.Infrastructure.Configuration.ConfigurationLoading;

public class DefaultRootConfigurationProvider : IRootConfigurationProvider
{
    private readonly Lazy<RadiusAdapterConfiguration> _rootConfig = new(() =>
    {
        var asm = Assembly.GetExecutingAssembly();
        var path = $"{asm.Location}.config";

        var rdsRootConfig = new RadiusConfigurationFile(path);
        var config = RadiusAdapterConfigurationFactory.Create(rdsRootConfig);

        return config;
    });

    public RadiusAdapterConfiguration GetRootConfiguration()
    {
        return _rootConfig.Value;
    }
}

internal static class RootConfigurationProvider
{
    private static readonly Lazy<RadiusAdapterConfiguration> _rootConfig = new(() =>
    {
        var asm = Assembly.GetExecutingAssembly();
        var path = $"{asm.Location}.config";

        var rdsRootConfig = new RadiusConfigurationFile(path);
        var config = RadiusAdapterConfigurationFactory.Create(rdsRootConfig);

        return config;
    });

    public static RadiusAdapterConfiguration GetRootConfiguration() => _rootConfig.Value;
}
