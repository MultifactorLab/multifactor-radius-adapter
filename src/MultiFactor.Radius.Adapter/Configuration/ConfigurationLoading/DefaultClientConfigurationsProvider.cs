//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MultiFactor.Radius.Adapter.Configuration.Core;
using MultiFactor.Radius.Adapter.Configuration.Models;
using MultiFactor.Radius.Adapter.Core;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using static MultiFactor.Radius.Adapter.Core.Literals;
using System.Reflection;
using Config = System.Configuration.Configuration;
using MultiFactor.Radius.Adapter.Core.Exceptions;
using System.Linq;

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

internal class XmlClientConfigurationsProvider
{
    private readonly ApplicationVariables _variables;
    private readonly ILogger<XmlClientConfigurationsProvider> _logger;

    public XmlClientConfigurationsProvider(ApplicationVariables variables, ILogger<XmlClientConfigurationsProvider> logger)
    {
        _variables = variables;
        _logger = logger;
    }

    public RadiusAdapterConfiguration[] GetClientConfigurations()
    {
        var clientConfigFilesPath = $"{_variables.AppPath}{Path.DirectorySeparatorChar}clients";
        var clientConfigFiles = Directory.Exists(clientConfigFilesPath) 
            ? Directory.GetFiles(clientConfigFilesPath, "*.config", SearchOption.AllDirectories) 
            : Array.Empty<string>();
        if (clientConfigFiles.Length == 0)
        {
            return Array.Empty<RadiusAdapterConfiguration>();
        }

        var list = new List<RadiusAdapterConfiguration>();
        foreach (var file in clientConfigFiles)
        {
            var name = Path.GetFileNameWithoutExtension(file);

            _logger.LogInformation("Loading client configuration '{name:l}' from {path:l}...",
                name,
                file);

            var builder = new ConfigurationBuilder().AddXmlFile(file);
            var configName = Path.GetFileNameWithoutExtension(file);
            var prefix = $"RADPTR_{configName.ToUpper()}";

            _logger.LogDebug("Adding environment variables with prefix '{prefix:l}'...", prefix);

            builder.AddEnvironmentVariables(prefix);

            _logger.LogDebug("Building and validating config '{name:l}'...", name);

            var configuration = builder.Build();
            var bounded = configuration.Get<RadiusAdapterConfiguration>();
            var validation = new RadiusAdapterConfigurationValidator().Validate(bounded);
            if (validation.IsValid)
            {
                _logger.LogDebug("Client configuration '{name:l}' was successfully added", name);
                continue;
            }

            var aggregatedMsg = validation.Errors
                .Select(x => x.ErrorMessage)
                .Aggregate((acc, cur) => $"{acc}{Environment.NewLine}{cur}");

            throw new InvalidConfigurationException(aggregatedMsg);
        }

        return list.ToArray();
    }
}