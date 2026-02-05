using Microsoft.Extensions.Configuration;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configurations.Models;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Configurations.Reader;

public static class ConfigurationReader
{
    public static ConfigurationFile Read(string filePath, string prefix = null)
    {
        var builder = new ConfigurationBuilder()
            .AddLegacyXmlConfig(filePath)
            .AddPrefixEnvironmentVariables($"RAD_{prefix}")
            .Build();

        try
        {
            var config = builder.Get<ConfigurationFile>();
            config.FileName =  Path.GetFileNameWithoutExtension(filePath);
            return config;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n=== Ошибка при Get<ConfigurationFile>(): {ex.Message} ===");
            Console.WriteLine("Подробности: " + ex.InnerException?.Message);
            throw;
        }

    }
}
    