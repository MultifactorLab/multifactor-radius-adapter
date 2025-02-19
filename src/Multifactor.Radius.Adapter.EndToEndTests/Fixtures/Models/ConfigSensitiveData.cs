namespace Multifactor.Radius.Adapter.EndToEndTests.Fixtures.Models;

public class ConfigSensitiveData
{
    public string ConfigName { get; }
    public Dictionary<string, string?> Data { get; }

    public ConfigSensitiveData(string configName, Dictionary<string, string?> data)
    {
        ConfigName = configName;
        Data = data;
    }

    public ConfigSensitiveData(string configName)
    {
        ConfigName = configName;
        Data = new Dictionary<string, string?>();
    }

    public void AddConfigValue(string key, string? value)
    {
        Data.Add(key, value);
    }
}

public static class ConfigSensitiveDataExtensions
{
    public static string? GetConfigValue(this ConfigSensitiveData[] configs, string configName, string fieldName)
    {
       var config = configs.First(x => x.ConfigName == configName);
       config.Data.TryGetValue(fieldName, out string? value);
       return value;
    }
}