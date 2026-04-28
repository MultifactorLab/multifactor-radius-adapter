using System.Globalization;
using System.Net;
using Multifactor.Core.Ldap.Name;
using Multifactor.Radius.Adapter.v2.Application.Core.Enum;
using Multifactor.Radius.Adapter.v2.Application.Core.Models;
using Multifactor.Radius.Adapter.v2.Infrastructure.Logging;
using NetTools;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Configurations.Parser;

internal static class ConfigurationValueParser
{
    public static bool TryParseEnum<T>(string? value, out T result, T defaultValue = default) where T : struct
    {
        result = defaultValue;
        
        if (string.IsNullOrWhiteSpace(value))
            return false;
        
        if (Enum.TryParse<T>(value, true, out var parsedResult))
        {
            result = parsedResult;
            return true;
        }
        
        return false;
    }
    
    public static bool TryParseTimeSpan(string? value, out TimeSpan result, TimeSpan? defaultValue = null)
    {
        result = defaultValue ?? TimeSpan.Zero;
        
        if (string.IsNullOrWhiteSpace(value))
            return false;
        
        if (TimeSpan.TryParse(value, out var parsedResult))
        {
            result = parsedResult;
            return true;
        }
        
        return false;
    }
    
    public static bool TryParseTimeout(string? value, out TimeSpan? result)
    {
        result = null;
        
        if (string.IsNullOrWhiteSpace(value))
            return false;
        
        var forced = value.EndsWith('!');
        if (forced)
            value = value.TrimEnd('!');
        
        if (!TimeSpan.TryParseExact(value, @"hh\:mm\:ss", null, TimeSpanStyles.None, out var parsedResult))
            return false;
        
        if (parsedResult == TimeSpan.Zero)
        {
            result = Timeout.InfiniteTimeSpan;
            return true;
        }
        
        // Логирование если timeout слишком маленький
        var recommendedMin = TimeSpan.FromSeconds(65);
        if (parsedResult < recommendedMin)
        {
            if (forced)
            {
                StartupLogger.Warning(
                    "Timeout {Timeout}s is less than recommended minimum {Recommended}s",
                    parsedResult.TotalSeconds, recommendedMin.TotalSeconds);
                result = parsedResult;
            }
            else
            {
                StartupLogger.Warning(
                    "Timeout {Timeout}s is less than recommended minimum {Recommended}s. Use 'value!' to force",
                    parsedResult.TotalSeconds, recommendedMin.TotalSeconds);
                result = recommendedMin;
            }
        }
        else
        {
            result = parsedResult;
        }
        
        return true;
    }
    
    public static bool TryParseEndpoint(string? value, out IPEndPoint? result)
    {
        result = null;
        
        if (string.IsNullOrWhiteSpace(value))
            return false;
        
        if (IPEndPoint.TryParse(value, out var endpoint))
        {
            result = endpoint;
            return true;
        }
        
        return false;
    }
    
    public static bool TryParseEndpoints(string? value, out IPEndPoint[] result, char separator = ';')
    {
        result = [];
        
        if (string.IsNullOrWhiteSpace(value))
            return false;
        
        var endpoints = new List<IPEndPoint>();
        var parts = value.Split(separator, StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var endpoint in parts)
        {
            if (IPEndPoint.TryParse(endpoint.Trim(), out var endpointResult))
            {
                endpoints.Add(endpointResult);
            }
            else
            {
                return false;
            }
        }
        
        result = endpoints.ToArray();
        return true;
    }

    public static bool TryParseUrls(string? value, out IReadOnlyList<Uri> result)
    {
        result = [];
        
        if (string.IsNullOrWhiteSpace(value))
            return false;
        
        var urls = new List<Uri>();
        var parts = value.Split(';', StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var part in parts)
        {
            var trimmed = part.Trim();
            if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri))
            {
                return false;
            }
            
            urls.Add(uri);
        }
        
        result = urls;
        return true;
    }
    
    public static bool TryParseDistinguishedNames(string? value, out IReadOnlyList<DistinguishedName> result)
    {
        result = [];
        
        if (string.IsNullOrWhiteSpace(value))
            return false;
        
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
            catch (ArgumentException)
            {
                return false;
            }
        }
        
        result = names;
        return true;
    }
    
    public static bool TryParseStringList(string? value, out IReadOnlyList<string> result, char separator = ';')
    {
        result = [];
        
        if (string.IsNullOrWhiteSpace(value))
            return false;
        
        result = value.Split(separator, StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .Where(s => !string.IsNullOrEmpty(s))
            .ToList();
            
        return true;
    }

    public static bool TryParsePrivacyModeWithFields(string? value, out Privacy? result)
    {
        result = null;
        
        if (string.IsNullOrWhiteSpace(value))
            return false;
        
        var parts = value.Split(':', 2);
        
        if (!TryParseEnum(parts[0], out PrivacyMode mode, PrivacyMode.None))
            return false;
        
        if (parts.Length == 1)
        {
            result = new Privacy(mode, []);
            return true;
        }
        
        var fields = parts[1].Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(f => f.Trim())
            .Distinct()
            .ToArray();
            
        result = new Privacy(mode, fields);
        return true;
    }
    
    public static bool TryParseDelaySettings(string? value, out CredentialDelay result)
    {
        result = new CredentialDelay(0, 0);
        
        if (string.IsNullOrWhiteSpace(value))
            return false;

        if (int.TryParse(value, out var delay))
        {
            if (delay < 0)
                return false;
                
            result =  new CredentialDelay(delay,delay);
            return true;
        }

        var splitted = value.Split(['-'], StringSplitOptions.RemoveEmptyEntries);
        if (splitted.Length != 2)
            return false;

        var values = splitted.Select(x => int.TryParse(x, out var d) ? d : -1).ToArray();
        if (values.Any(x => x < 0))
            return false;
            
        result = new CredentialDelay(values[0], values[1]);
        return true;
    }
    
    public static bool TryParseIpEntries(string? value, out IpEntry[]? entries)
    {
        entries = null;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var parts = value.Split(';', StringSplitOptions.RemoveEmptyEntries);
        entries = new IpEntry[parts.Length];

        for (int i = 0; i < parts.Length; i++)
        {
            var trimmed = parts[i].Trim();

            if (trimmed.Contains('-'))
            {
                var range = trimmed.Split('-');
                if (!IPAddress.TryParse(range[0].Trim(), out var s) ||
                    !IPAddress.TryParse(range[1].Trim(), out var e))
                    return false;
                entries[i] = IpEntry.Range(s, e);
            }
            else if (trimmed.Contains('/'))
            {
                var cidr = trimmed.Split('/');
                if (!IPAddress.TryParse(cidr[0].Trim(), out var n) ||
                    !int.TryParse(cidr[1].Trim(), out var p))
                    return false;
            
                int maxPrefix = n.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6 ? 128 : 32;
                if (p < 0 || p > maxPrefix)
                    return false;
                
                entries[i] = IpEntry.Cidr(n, p);
            }
            else
            {
                if (!IPAddress.TryParse(trimmed, out var ip))
                    return false;
                entries[i] = IpEntry.Single(ip);
            }
        }

        return true;
    }
}