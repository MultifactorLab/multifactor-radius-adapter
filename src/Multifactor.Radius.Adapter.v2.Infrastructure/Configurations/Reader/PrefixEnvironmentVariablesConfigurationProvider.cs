using System.Collections;
using Microsoft.Extensions.Configuration;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Configurations.Reader;

internal sealed class PrefixEnvironmentVariablesConfigurationProvider : ConfigurationProvider
{
    private readonly string _prefix;
    
    public PrefixEnvironmentVariablesConfigurationProvider(string? prefix)
    {
        _prefix = prefix ?? string.Empty;
    }
    
    public override void Load()
    {
        Data.Clear();
        var envVars = Environment.GetEnvironmentVariables();
        
        foreach (DictionaryEntry entry in envVars)
        {
            var key = entry.Key.ToString();
            if (string.IsNullOrEmpty(key) || !key.StartsWith(_prefix, StringComparison.OrdinalIgnoreCase)) continue;
            var value = entry.Value?.ToString();
            if (value == null) continue;
            var configKey = key[_prefix.Length..]
                .Replace("__", ":") 
                .ToLower();
                    
            Data[configKey] = value;
        }
    }
}