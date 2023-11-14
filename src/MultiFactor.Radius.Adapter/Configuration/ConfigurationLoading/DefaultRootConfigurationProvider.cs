//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using System;
using System.Configuration;
using MultiFactor.Radius.Adapter.Configuration.Core;
using Config = System.Configuration.Configuration;

namespace MultiFactor.Radius.Adapter.Configuration.ConfigurationLoading;

public class DefaultRootConfigurationProvider : IRootConfigurationProvider
{
    private Lazy<Config> _rootConfig = new Lazy<Config>(() =>
    {
        return ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
    });

    public Config GetRootConfiguration()
    {
        return _rootConfig.Value;
    }
}