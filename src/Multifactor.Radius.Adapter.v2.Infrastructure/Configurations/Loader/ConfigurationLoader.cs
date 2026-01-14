using System.Reflection;
using Microsoft.Extensions.Logging;
using Multifactor.Radius.Adapter.v2.Application.Configuration.Models;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configurations.Exceptions;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configurations.Parser;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Configurations.Loader;

public class ConfigurationLoader : IConfigurationLoader
{
    private readonly IConfigurationParser _parser;
    private readonly ILogger<ConfigurationLoader> _logger;
    
    public ConfigurationLoader(
        IConfigurationParser parser,
        ILogger<ConfigurationLoader> logger)
    {
        _parser = parser;
        _logger = logger;
    }
    
    public async Task<ServiceConfiguration> LoadAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Loading configuration...");
        
        var rootConfig = await LoadRootConfigurationAsync(cancellationToken);
        
        var clients = await LoadClientConfigurationsAsync(cancellationToken);
        
        var serviceConfig = new ServiceConfiguration
        {
            RootConfiguration = rootConfig,
            ClientsConfigurations = clients
        };
        
        _logger.LogInformation("Configuration loaded: {ClientCount} clients", clients.Count);
        
        return serviceConfig;
    }
    
    private async Task<RootConfiguration> LoadRootConfigurationAsync(CancellationToken ct)
    {
        var assemblyLocation = Assembly.GetEntryAssembly()?.Location;
        var configPath = $"{assemblyLocation}.config";
        
        if (!File.Exists(configPath))
            throw new InvalidConfigurationException($"Root configuration not found: {configPath}");
        
        return await _parser.ParseRootConfigAsync(configPath, ct);
    }
    
    private async Task<List<ClientConfiguration>> LoadClientConfigurationsAsync(
        CancellationToken ct)
    {
        var clientsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "clients");
        var clients = new List<ClientConfiguration>();
        
        if (!Directory.Exists(clientsPath))
        {
            _logger.LogDebug("No clients directory found at {Path}", clientsPath);
            return clients;
        }
        
        var configFiles = Directory.GetFiles(clientsPath, "*.config");
        
        foreach (var file in configFiles)
        {
            try
            {
                var clientDto = await _parser.ParseClientConfigAsync(file, ct);
                clients.Add(clientDto);
                
                _logger.LogDebug("Loaded client: {Name} from {File}", clientDto.Name, Path.GetFileName(file));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load client configuration from {File}", file);
                throw;
            }
        }
        
        return clients;
    }
}