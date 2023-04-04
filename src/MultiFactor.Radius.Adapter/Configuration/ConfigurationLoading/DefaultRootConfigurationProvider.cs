//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using System;
using System.Configuration;

namespace MultiFactor.Radius.Adapter.Configuration.ConfigurationLoading
{
    public class DefaultRootConfigurationProvider : IRootConfigurationProvider
    {
        private Lazy<System.Configuration.Configuration> _rootConfig = new Lazy<System.Configuration.Configuration>(() =>
        {
            return ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
        });

        public System.Configuration.Configuration GetRootConfiguration()
        {
            return _rootConfig.Value;
        }
    }
}