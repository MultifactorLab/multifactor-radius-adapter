//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MultiFactor.Radius.Adapter.Configuration.Models;
using MultiFactor.Radius.Adapter.Core;
using System;
using System.Collections.Generic;
using System.IO;
using MultiFactor.Radius.Adapter.Core.Exceptions;
using System.Linq;

namespace MultiFactor.Radius.Adapter.Configuration.ConfigurationLoading;

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
            if (bounded == null)
            {
                throw new Exception($"Failed to bind {name} configuration");
            }

            var validation = new RadiusAdapterConfigurationValidator().Validate(bounded);
            if (validation.IsValid)
            {
                list.Add(bounded);
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