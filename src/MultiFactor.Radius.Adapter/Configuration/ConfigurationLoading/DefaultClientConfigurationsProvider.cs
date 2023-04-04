﻿//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using MultiFactor.Radius.Adapter.Core;
using Serilog;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;

namespace MultiFactor.Radius.Adapter.Configuration.ConfigurationLoading
{
    public class DefaultClientConfigurationsProvider : IClientConfigurationsProvider
    {
        private readonly ApplicationVariables _variables;
        private readonly ILogger _logger;

        public DefaultClientConfigurationsProvider(ApplicationVariables variables, ILogger logger)
        {
            _variables = variables ?? throw new ArgumentNullException(nameof(variables));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public System.Configuration.Configuration[] GetClientConfigurations()
        {
            var clientConfigFilesPath = $"{_variables.AppPath}{Path.DirectorySeparatorChar}clients";
            var clientConfigFiles = Directory.Exists(clientConfigFilesPath) ? Directory.GetFiles(clientConfigFilesPath, "*.config") : new string[0];
            if (clientConfigFiles.Length == 0) return new System.Configuration.Configuration[0];

            var list = new List<System.Configuration.Configuration>();
            foreach (var file in clientConfigFiles)
            {
                _logger.Information($"Loading client configuration from {Path.GetFileName(file)}");

                var customConfigFileMap = new ExeConfigurationFileMap();
                customConfigFileMap.ExeConfigFilename = file;
                list.Add(ConfigurationManager.OpenMappedExeConfiguration(customConfigFileMap, ConfigurationUserLevel.None));
            }

            return list.ToArray();
        }
    }
}