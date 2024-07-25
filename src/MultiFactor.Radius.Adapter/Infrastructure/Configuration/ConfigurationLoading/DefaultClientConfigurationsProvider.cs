//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using Microsoft.Extensions.Logging;
using MultiFactor.Radius.Adapter.Core;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration.XmlAppConfiguration;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MultiFactor.Radius.Adapter.Infrastructure.Configuration.ConfigurationLoading;

public class DefaultClientConfigurationsProvider : IClientConfigurationsProvider
{
    private readonly Lazy<Dictionary<RadiusConfigurationFile, RadiusAdapterConfiguration>> _loaded;
    private readonly ApplicationVariables _variables;
    private readonly ILogger<DefaultClientConfigurationsProvider> _logger;

    public DefaultClientConfigurationsProvider(ApplicationVariables variables, ILogger<DefaultClientConfigurationsProvider> logger)
    {
        _variables = variables ?? throw new ArgumentNullException(nameof(variables));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _loaded = new Lazy<Dictionary<RadiusConfigurationFile, RadiusAdapterConfiguration>>(Load);
    }

    public RadiusAdapterConfiguration[] GetClientConfigurations() => _loaded.Value.Select(x => x.Value).ToArray();

    public RadiusConfigurationFile GetSource(RadiusAdapterConfiguration configuration)
    {
        var pair = _loaded.Value.FirstOrDefault(x => x.Value == configuration);
        // default (KeyValuePair<RadiusConfigurationFile, RadiusAdapterConfiguration>) is KeyValuePair<null, null>
        return pair.Key;
    }

    private Dictionary<RadiusConfigurationFile, RadiusAdapterConfiguration> Load()
    {
        var clientConfigFilesPath = $"{_variables.AppPath}{Path.DirectorySeparatorChar}clients";
        var clientConfigFiles = Directory.Exists(clientConfigFilesPath)
            ? Directory.GetFiles(clientConfigFilesPath, "*.config")
            : Array.Empty<string>();

        var dict = new Dictionary<RadiusConfigurationFile, RadiusAdapterConfiguration>();
        if (clientConfigFiles.Length == 0)
        {
            return dict;
        }

        foreach (var file in clientConfigFiles.Select(x => new RadiusConfigurationFile(x)))
        {
            _logger.LogInformation("Loading client configuration from {path:l}", file);

            var config = RadiusAdapterConfigurationFactory.Create(file, file.NameWithoutExtension);
            dict.Add(file, config);
        }

        return dict;
    }
}
