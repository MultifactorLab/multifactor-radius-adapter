//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using System;
using System.Configuration;

namespace MultiFactor.Radius.Adapter.Configuration.ConfigurationLoading
{
    public class DefaultAppConfigurationProvider : IAppConfigurationProvider
    {
        public System.Configuration.Configuration GetClientConfiguration(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException($"'{nameof(path)}' cannot be null or whitespace.", nameof(path));
            }

            var customConfigFileMap = new ExeConfigurationFileMap();
            customConfigFileMap.ExeConfigFilename = path;

            return ConfigurationManager.OpenMappedExeConfiguration(customConfigFileMap, ConfigurationUserLevel.None);
        }

        public System.Configuration.Configuration GetRootConfiguration()
        {
            return ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
        }
    }
}