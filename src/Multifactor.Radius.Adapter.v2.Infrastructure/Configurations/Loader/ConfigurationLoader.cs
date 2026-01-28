using System.Reflection;
using Microsoft.Extensions.Logging;
using Multifactor.Radius.Adapter.v2.Application.Configuration.Models;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configurations.Exceptions;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configurations.Parser;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Configurations.Loader;

public class ConfigurationLoader : IConfigurationLoader
{
    private readonly IConfigurationParser _parser;
    
    public ConfigurationLoader(
        IConfigurationParser parser)
    {
        _parser = parser;
    }
    
    public ServiceConfiguration Load()
    {
        return Task.Run(() => LoadAsync(CancellationToken.None)).GetAwaiter().GetResult();
    }
    
    public async Task<ServiceConfiguration> LoadAsync(CancellationToken cancellationToken)
    {
        var rootConfig = await LoadRootConfigurationAsync(cancellationToken);
        var clients = await LoadClientConfigurationsAsync(cancellationToken);
        var serviceConfig = new ServiceConfiguration
        {
            RootConfiguration = rootConfig,
            ClientsConfigurations = clients
        };
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
            return clients;
        }
        
        var configFiles = Directory.GetFiles(clientsPath, "*.config");
        if (configFiles.Length == 0)
        {
            var assemblyLocation = Assembly.GetEntryAssembly()?.Location;
            var configPath = $"{assemblyLocation}.config";
        
            if (!File.Exists(configPath))
                throw new InvalidConfigurationException($"Root configuration not found: {configPath}");
            
            return [await _parser.ParseClientConfigAsync(configPath, ct)];
        }
        foreach (var file in configFiles)
        {
            var clientDto = await _parser.ParseClientConfigAsync(file, ct);
            clients.Add(clientDto);
        }
        
        return clients;
    }
}