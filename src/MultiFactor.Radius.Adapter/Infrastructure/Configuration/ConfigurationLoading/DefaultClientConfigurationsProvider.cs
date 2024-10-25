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
using System.Text.RegularExpressions;

namespace MultiFactor.Radius.Adapter.Infrastructure.Configuration.ConfigurationLoading;

public class DefaultClientConfigurationsProvider : IClientConfigurationsProvider
{
    private readonly Lazy<Dictionary<RadiusConfigurationSource, RadiusAdapterConfiguration>> _loaded;
    private readonly ApplicationVariables _variables;
    private readonly ILogger<DefaultClientConfigurationsProvider> _logger;

    public DefaultClientConfigurationsProvider(ApplicationVariables variables, 
        ILogger<DefaultClientConfigurationsProvider> logger)
    {
        _variables = variables ?? throw new ArgumentNullException(nameof(variables));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _loaded = new Lazy<Dictionary<RadiusConfigurationSource, RadiusAdapterConfiguration>>(Load);
    }

    public RadiusAdapterConfiguration[] GetClientConfigurations() => _loaded.Value.Select(x => x.Value).ToArray();

    public RadiusConfigurationSource GetSource(RadiusAdapterConfiguration configuration)
    {
        var pair = _loaded.Value.FirstOrDefault(x => x.Value == configuration);
        // default (KeyValuePair<RadiusConfigurationFile, RadiusAdapterConfiguration>) is KeyValuePair<null, null>
        return pair.Key;
    }

    private Dictionary<RadiusConfigurationSource, RadiusAdapterConfiguration> Load()
    {
        var clientConfigFilesPath = $"{_variables.AppPath}{Path.DirectorySeparatorChar}clients";
        var clientConfigFiles = Directory.Exists(clientConfigFilesPath)
            ? Directory.GetFiles(clientConfigFilesPath, "*.config")
            : Array.Empty<string>();

        var dict = new Dictionary<RadiusConfigurationSource, RadiusAdapterConfiguration>();

        var fileSources = clientConfigFiles.Select(x => new RadiusConfigurationFile(x)).ToArray();
        foreach (var file in fileSources)
        {
            _logger.LogInformation("Loading client configuration from {path:l}", file);

            var config = RadiusAdapterConfigurationFactory.Create(file, file.Name);
            dict.Add(file, config);
        }
        
        var envVarSources = GetEnvVarClients()
            .Select(x => new RadiusConfigurationEnvironmentVariable(x))
            .ExceptBy(fileSources.Select(x => RadiusConfigurationSource.TransformName(x.Name)), x => x.Name);
        foreach (var envVarClient in envVarSources)
        {
            _logger.LogInformation("Found environment variable client '{Client:l}'", envVarClient);
            
            var config = RadiusAdapterConfigurationFactory.Create(envVarClient);
            dict.Add(envVarClient, config);
        }
        
        return dict;
    }

    private static IEnumerable<string> GetEnvVarClients()
    {
        var patterns = RadiusAdapterConfiguration.KnownSectionNames
            .Select(x => $"^(?i){ConfigurationBuilderExtensions.BasePrefix}(?<cli>[a-zA-Z_]+[a-zA-Z0-9_]*)_{x}")
            .ToArray();
        
        var keys = Environment.GetEnvironmentVariables().Keys
            .Cast<string>()
            .Where(x => x.StartsWith(ConfigurationBuilderExtensions.BasePrefix, StringComparison.OrdinalIgnoreCase));
        
        foreach (var key in keys)
        {
            var groupCollection = patterns.Select(x => Regex.Match(key, x).Groups).FirstOrDefault(x => x.Count != 0);
            if (groupCollection is null)
            {
                continue;
            }

            if (!groupCollection.TryGetValue("cli", out var cli))
            {
                continue;
            }

            yield return cli.Value;
        }
    }
}
