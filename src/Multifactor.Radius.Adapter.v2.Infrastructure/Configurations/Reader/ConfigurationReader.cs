using Microsoft.Extensions.Configuration;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configurations.Models;
using Multifactor.Radius.Adapter.v2.Infrastructure.Logging;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Configurations.Reader;

internal static class ConfigurationReader
{
    internal static AdapterConfiguration? Read(string filePath, string? prefix = null)
    {
        var builder = new ConfigurationBuilder()
            .AddXmlConfig(filePath)
            .AddEnvironmentVariables($"RAD_{prefix}")
            .Build();

        try
        {
            var config = builder.Get<AdapterConfiguration>();
            if (config == null) return null;
            config.FileName =  Path.GetFileNameWithoutExtension(filePath);
            return config;
        }
        catch (Exception ex)
        {
            StartupLogger.Error(ex, "Error reading configuration file:{0}", ex.Message);
            throw;
        }

    }
}
    