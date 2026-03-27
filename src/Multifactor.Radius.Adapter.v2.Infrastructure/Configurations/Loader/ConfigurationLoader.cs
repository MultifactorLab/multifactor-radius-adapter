using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using Multifactor.Radius.Adapter.v2.Application.Core.Models;
using Multifactor.Radius.Adapter.v2.Application.Core.Models.Abstractions;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configurations.Exceptions;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configurations.Models;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configurations.Models.Dictionary;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configurations.Models.Dictionary.Attributes;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configurations.Reader;
using Multifactor.Radius.Adapter.v2.Infrastructure.Logging;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Configurations.Loader;

public interface IConfigurationLoader
{
    ServiceConfiguration Load(); 
}

public class ConfigurationLoader : IConfigurationLoader
{
    private readonly IRadiusDictionary _dictionary;
    
    public ConfigurationLoader(IRadiusDictionary dictionary)
    {
        _dictionary = dictionary;
    }
    
    public ServiceConfiguration Load()
    {
        StartupLogger.Information("===== Multifactor RADIUS Adapter =====");
        StartupLogger.Information("Application initialization started");
        StartupLogger.Information(_dictionary.GetInfo());
        var rootConfigPath = GetRootConfigPath();
        var rootConfig = LoadRootConfiguration(rootConfigPath);
        var clients = LoadClientConfigurations(rootConfigPath, out var isRootMode);
        
        return new ServiceConfiguration
        {
            RootConfiguration = rootConfig,
            ClientsConfigurations = clients,
            IsRootClientMode = isRootMode
        };
    }
    
    private static string GetRootConfigPath()
    {
        var assemblyLocation = Assembly.GetEntryAssembly()?.Location;
        return $"{assemblyLocation}.config";
    }
    
    private static RootConfiguration LoadRootConfiguration(string configPath)
    {
        if (!File.Exists(configPath))
            throw new InvalidConfigurationException($"Root configuration not found: {configPath}");
        var fileName = Path.GetFileNameWithoutExtension(configPath);
        StartupLogger.Information($"Loading root configuration from '{fileName}'");
        var config = ReadConfiguration(configPath);
        return RootConfiguration.FromConfiguration(config);
    }
    
    private List<ClientConfiguration> LoadClientConfigurations(string rootConfigPath, out bool isRootMode)
    {
        var clientsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "clients");
        
        if (!Directory.Exists(clientsPath))
        {
            var clientConfig = ParseClientConfiguration(rootConfigPath, true);
            isRootMode = true;
            return [clientConfig];
        }
        
        var clientConfigFiles = Directory.GetFiles(clientsPath, "*.config");
        
        if (clientConfigFiles.Length == 0)
        {
            var clientConfig = ParseClientConfiguration(rootConfigPath, true);
            isRootMode = true;
            return [clientConfig];
        }

        isRootMode = false;
        return clientConfigFiles
            .Select(c => ParseClientConfiguration(c))
            .ToList();
    }
    
    private ClientConfiguration ParseClientConfiguration(string filePath, bool isRoot = false)
    {
        var fileName = Path.GetFileNameWithoutExtension(filePath);
        StartupLogger.Information($"Loading client configuration from '{fileName}'");
        var prefix = GetConfigPrefix(filePath);
        var config = ReadConfiguration(filePath, prefix);
        
        var clientConfig = ClientConfiguration.FromConfiguration(config, isRoot);
        clientConfig.ReplyAttributes = ParseReplyAttributes(config.RadiusReply);
        clientConfig.LdapServers = config.LdapServers.Select(conf => LdapServerConfiguration.FromConfiguration(conf, clientConfig.Name)).ToList();
        
        return clientConfig;
    }
    
    private static AdapterConfiguration? ReadConfiguration(string filePath, string? prefix = null)
    {
        if (!File.Exists(filePath))
            throw new InvalidConfigurationException($"Configuration file not found: {filePath}");
            
        return ConfigurationReader.Read(filePath, prefix);
    }
    
    private IReadOnlyDictionary<string, IReadOnlyList<IRadiusReplyAttribute>> ParseReplyAttributes(
        RadiusReplySection radiusReplySection)
    {
        if (radiusReplySection?.Attributes?.Any() != true)
            return new Dictionary<string, IReadOnlyList<IRadiusReplyAttribute>>();
        
        var result = new Dictionary<string, IReadOnlyList<IRadiusReplyAttribute>>();
        
        var groupedAttributes = radiusReplySection.Attributes
            .Where(a => !string.IsNullOrWhiteSpace(a.Name))
            .GroupBy(a => a.Name!);
        
        foreach (var group in groupedAttributes)
        {
            var attributes = group
                .Select(CreateReplyAttribute)
                .ToList();
            
            result[group.Key] = attributes;
        }
        
        return result;
    }
    
    private IRadiusReplyAttribute CreateReplyAttribute(RadiusAttributeItem item)
    {
        var attribute = new RadiusReplyAttribute();
        
        if (bool.TryParse(item.Sufficient, out var sufficient))
        {
            attribute.Sufficient = sufficient;
        }
        
        if (!string.IsNullOrWhiteSpace(item.From))
        {
            attribute.Name = item.From;
        }
        else if (!string.IsNullOrWhiteSpace(item.Value))
        {
            attribute.Value = ParseRadiusReplyValue(item.Name!, item.Value);
            
            if (!string.IsNullOrWhiteSpace(item.When))
            {
                ParseWhenCondition(item.When, attribute);
            }
        }
        
        return attribute;
    }
    
    private object ParseRadiusReplyValue(string attributeName, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidConfigurationException("Radius reply value must be specified");
        
        var attribute = _dictionary.GetAttribute(attributeName);
        
        return attribute.Type switch
        {
            DictionaryAttribute.TypeString or DictionaryAttribute.TypeTaggedString => value,
            DictionaryAttribute.TypeInteger or DictionaryAttribute.TypeTaggedInteger => uint.Parse(value),
            DictionaryAttribute.TypeIpAddr => IPAddress.Parse(value),
            DictionaryAttribute.TypeOctet => ToByteArray(value),
            _ => throw new InvalidConfigurationException($"Unknown attribute type: {attribute.Type}")
        };
    }
    
    private static void ParseWhenCondition(string whenCondition, RadiusReplyAttribute attribute)
    {
        var parts = whenCondition.Split('=', 2, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2) return;
        
        var conditionType = parts[0].Trim();
        var values = parts[1]
            .Split(';', StringSplitOptions.RemoveEmptyEntries)
            .Select(v => v.Trim())
            .ToList();
        
        switch (conditionType)
        {
            case "UserGroup":
                attribute.UserGroupCondition = values;
                break;
            case "UserName":
                attribute.UserNameCondition = values;
                break;
            default:
                throw new InvalidConfigurationException($"Unknown condition type: {conditionType}");
        }
    }
    
    private static string GetConfigPrefix(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return string.Empty;
        
        var fileName = Path.GetFileNameWithoutExtension(filePath);
        return Regex.Replace(fileName, @"\s+", string.Empty);
    }
    
    private static byte[] ToByteArray(string hex)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(hex);
        
        var bytes = new byte[hex.Length / 2];
        for (int i = 0; i < hex.Length; i += 2)
        {
            bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
        }

        return bytes;
    }
}