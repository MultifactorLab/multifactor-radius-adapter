//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using MultiFactor.Radius.Adapter.Configuration.Core;
using System;
using System.Configuration;
using Config = System.Configuration.Configuration;

namespace MultiFactor.Radius.Adapter.Configuration.ConfigurationLoading;

public class DefaultRootConfigurationProvider : IRootConfigurationProvider
{
    private readonly Lazy<Config> _rootConfig = new(() =>
    {
        return ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
    });

    public Config GetRootConfiguration()
    {
        return _rootConfig.Value;
    }
}
