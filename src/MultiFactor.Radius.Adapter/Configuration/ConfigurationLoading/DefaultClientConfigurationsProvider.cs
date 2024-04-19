//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using Microsoft.Extensions.Logging;
using MultiFactor.Radius.Adapter.Configuration.Core;
using MultiFactor.Radius.Adapter.Core;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using Config = System.Configuration.Configuration;

namespace MultiFactor.Radius.Adapter.Configuration.ConfigurationLoading;

public class DefaultClientConfigurationsProvider : IClientConfigurationsProvider
{
    private readonly ApplicationVariables _variables;
    private readonly ILogger<DefaultClientConfigurationsProvider> _logger;

    public DefaultClientConfigurationsProvider(ApplicationVariables variables, ILogger<DefaultClientConfigurationsProvider> logger)
    {
        _variables = variables ?? throw new ArgumentNullException(nameof(variables));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Config[] GetClientConfigurations()
    {
        var clientConfigFilesPath = $"{_variables.AppPath}{Path.DirectorySeparatorChar}clients";
        var clientConfigFiles = Directory.Exists(clientConfigFilesPath) ? Directory.GetFiles(clientConfigFilesPath, "*.config") : Array.Empty<string>();
        if (clientConfigFiles.Length == 0) return Array.Empty<Config>();

        var list = new List<Config>();
        foreach (var file in clientConfigFiles)
        {
            _logger.LogInformation("Loading client configuration from {path}", Path.GetFileName(file));

            var customConfigFileMap = new ExeConfigurationFileMap
            {
                ExeConfigFilename = file
            };
            list.Add(ConfigurationManager.OpenMappedExeConfiguration(customConfigFileMap, ConfigurationUserLevel.None));
        }

        return list.ToArray();
    }
}