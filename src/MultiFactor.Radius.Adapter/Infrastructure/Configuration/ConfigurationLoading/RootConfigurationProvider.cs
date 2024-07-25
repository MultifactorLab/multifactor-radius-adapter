﻿//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using MultiFactor.Radius.Adapter.Infrastructure.Configuration.XmlAppConfiguration;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration.Models;
using System;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration.RootLevel;

namespace MultiFactor.Radius.Adapter.Infrastructure.Configuration.ConfigurationLoading;

internal static class RootConfigurationProvider
{
    private static readonly Lazy<RadiusAdapterConfiguration> _rootConfig = new(() =>
    {
        var path = RootConfigurationFile.Path;

        var rdsRootConfig = new RadiusConfigurationFile(path);
        var config = RadiusAdapterConfigurationFactory.Create(rdsRootConfig);

        return config;
    });

    public static RadiusAdapterConfiguration GetRootConfiguration() => _rootConfig.Value;
}
