using System.Globalization;
using System.Net;
using Multifactor.Core.Ldap.Name;
using Multifactor.Radius.Adapter.v2.Application.Configuration.Models.Enum;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configurations.Exceptions;
using Multifactor.Radius.Adapter.v2.Infrastructure.Logging;
using NetTools;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Configurations.Parser;

public static class ValueParser
{
    public static T ParseEnum<T>(string? value, T defaultValue = default, bool required = false) where T : struct
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return required ? throw new InvalidConfigurationException($"Enum value of type {typeof(T).Name} is required") : defaultValue;
        }
        
        return Enum.TryParse<T>(value, true, out var result) ? result 
            : throw new InvalidConfigurationException($"Invalid value '{value}' for enum {typeof(T).Name}");
    }
    
    public static bool ParseBool(string? value, bool defaultValue)
    {
        if (string.IsNullOrWhiteSpace(value))
            return defaultValue;
        
        return bool.TryParse(value, out var result) ? result : defaultValue;
    }
    
    public static int ParseInt(string? value, int defaultValue)
    {
        if (string.IsNullOrWhiteSpace(value))
            return defaultValue;
        
        return int.TryParse(value, out var result) ? result : defaultValue;
    }
    
    public static TimeSpan ParseTimeSpan(string? value, TimeSpan? defaultValue = null)
    {
        if (string.IsNullOrWhiteSpace(value))
            return defaultValue ?? TimeSpan.Zero;
        
        if (TimeSpan.TryParse(value, out var result))
            return result;
            
        throw new InvalidConfigurationException($"Invalid time span format: '{value}'");
    }
    
    public static TimeSpan ParseTimeout(string? value, TimeSpan defaultValue)
    {
        if (string.IsNullOrWhiteSpace(value))
            return defaultValue;
        
        var forced = value.EndsWith('!');
        if (forced)
            value = value.TrimEnd('!');
        
        if (!TimeSpan.TryParseExact(value, @"hh\:mm\:ss", null, TimeSpanStyles.None, out var result))
            return defaultValue;
        
        if (result == TimeSpan.Zero)
            return Timeout.InfiniteTimeSpan;
        
        // Логирование если timeout слишком маленький
        var recommendedMin = TimeSpan.FromSeconds(65);
        if (result < recommendedMin)
        {
            if (forced)
            {
                StartupLogger.Warning(
                    "Timeout {Timeout}s is less than recommended minimum {Recommended}s",
                    result.TotalSeconds, recommendedMin.TotalSeconds);
            }
            else
            {
                StartupLogger.Warning(
                    "Timeout {Timeout}s is less than recommended minimum {Recommended}s. Use 'value!' to force",
                    result.TotalSeconds, recommendedMin.TotalSeconds);
                result = recommendedMin;
            }
        }
        
        return result;
    }
    
    public static IPEndPoint? ParseEndpoint(string? value, bool required = false)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return required 
                ? throw new InvalidConfigurationException("Endpoint is required") : null;
        }
        
        return IPEndPoint.TryParse(value, out var endpoint) ? endpoint 
            : throw new InvalidConfigurationException($"Invalid endpoint format: '{value}'");
    }
    
    public static IPEndPoint[] ParseEndpoints(string? value, char separator = ';', bool required = false)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return required 
                ? throw new InvalidConfigurationException("Endpoints is required") : [];
        }
        
        return value.Split(separator).Select(endpoint => IPEndPoint.TryParse(endpoint, out var endpointResult) ? endpointResult
            : throw new InvalidConfigurationException($"Invalid endpoint format: '{endpoint}'")).ToArray();
        
    }

    public static IPAddress? ParseIpAddress(string? value, bool required = false)
    {
        return IPAddress.TryParse(value, out var result) ? result 
            : throw new InvalidConfigurationException($"Invalid IP address format: '{value}'");
    }

    public static IReadOnlyList<Uri> ParseUrls(string? value, bool required = false)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return required ? throw new InvalidConfigurationException("URLs are required") : [];
        }
        
        var urls = new List<Uri>();
        var parts = value.Split(';', StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var part in parts)
        {
            var trimmed = part.Trim();
            if (Uri.TryCreate(trimmed, UriKind.Absolute, out var uri))
            {
                urls.Add(uri);
            }
            else
            {
                throw new InvalidConfigurationException($"Invalid URL format: '{trimmed}'");
            }
        }
        
        return urls;
    }
    
    public static IReadOnlyList<IPAddressRange> ParseIpRanges(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return [];
        
        var ranges = new List<IPAddressRange>();
        var parts = value.Split(';', StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var part in parts)
        {
            var trimmed = part.Trim();
            if (IPAddressRange.TryParse(trimmed, out var range))
            {
                ranges.Add(range);
            }
            else
            {
                throw new InvalidConfigurationException($"Invalid IP address range: '{trimmed}'");
            }
        }
        
        return ranges;
    }
    
    public static IReadOnlyList<DistinguishedName> ParseDistinguishedNames(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return [];
        
        var names = new List<DistinguishedName>();
        var parts = value.Split(';', StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var part in parts)
        {
            try
            {
                var trimmed = part.Trim();
                if (!string.IsNullOrEmpty(trimmed))
                {
                    names.Add(new DistinguishedName(trimmed));
                }
            }
            catch (ArgumentException ex)
            {
                throw new InvalidConfigurationException($"Invalid distinguished name: '{part}'", ex);
            }
        }
        
        return names;
    }
    
    public static IReadOnlyList<string> ParseStringList(string? value, char separator = ';')
    {
        if (string.IsNullOrWhiteSpace(value))
            return [];
        
        return value.Split(separator, StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .Where(s => !string.IsNullOrEmpty(s))
            .ToList();
    }

    
    public static (PrivacyMode Mode, string[] Fields) ParsePrivacyModeWithFields(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return (PrivacyMode.None, []);
        
        var parts = value.Split(':', 2);
        var mode = ParseEnum(parts[0], PrivacyMode.None);
        
        if (parts.Length == 1)
            return (mode, []);
        
        var fields = parts[1].Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(f => f.Trim())
            .Distinct()
            .ToArray();
            
        return (mode, fields);
    }
    
    public static (int min, int max) ParseDelaySettings(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return new (0, 0);
        }

        if (int.TryParse(value, out var delay))
        {
            return delay < 0 ? throw new InvalidConfigurationException($"Invalid delay setting: '{value}'") 
                : new (delay, delay);
        }

        var splitted = value.Split(['-'], StringSplitOptions.RemoveEmptyEntries);
        if (splitted.Length != 2) throw new InvalidConfigurationException($"Invalid delay setting: '{value}'");

        var values = splitted.Select(x => int.TryParse(x, out var d) ? d : -1).ToArray();
        return values.Any(x => x < 0) ? throw new InvalidConfigurationException($"Invalid delay setting: '{value}'") 
            : new (values[0], values[1]);
    }
}