using System.Net;
using Multifactor.Radius.Adapter.v2.Application.Configuration.Models;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configurations.Exceptions;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configurations.Parser;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Configurations.Models;

public class RootConfiguration : IRootConfiguration
{    
    public IReadOnlyList<Uri> MultifactorApiUrls { get; set; }
    public string? MultifactorApiProxy { get; set; }
    public TimeSpan MultifactorApiTimeout { get; set; }
    public IPEndPoint? AdapterServerEndpoint { get; set; }
    public string LoggingLevel { get; set; }
    public string? LoggingFormat { get; set; }
    public bool SyslogUseTls { get; set; }
    public string? SyslogServer { get; set; }
    public string? SyslogFormat { get; set; }
    public string? SyslogFacility { get; set; }
    public string SyslogAppName { get; set; }
    public string? SyslogFramer { get; set; }
    public string? SyslogOutputTemplate { get; set; }
    
    public string? ConsoleLogOutputTemplate { get; set; }
    public string? FileLogOutputTemplate { get; set; }
    public int LogFileMaxSizeBytes { get; set; }

    public static RootConfiguration FromConfiguration(ConfigurationFile configurationFile)
    {
        ArgumentNullException.ThrowIfNull(configurationFile);
        var conf = new RootConfiguration
        {
            MultifactorApiProxy = configurationFile.AppSettings.MultifactorApiProxy,
            MultifactorApiTimeout = ConfigurationValueProcessor.TryParseTimeout(
                configurationFile.AppSettings.MultifactorApiTimeout, out var span)
                ? span!.Value : TimeSpan.FromSeconds(65),
            LoggingFormat = configurationFile.AppSettings.LoggingFormat,
            SyslogUseTls = configurationFile.AppSettings.SyslogUseTls,
            SyslogServer = configurationFile.AppSettings.SyslogServer,
            SyslogFormat =  configurationFile.AppSettings.SyslogFormat, 
            SyslogFacility = configurationFile.AppSettings.SyslogFacility,
            SyslogAppName = configurationFile.AppSettings.SyslogAppName ?? "multifactor-radius",
            SyslogFramer = configurationFile.AppSettings.SyslogFramer,
            SyslogOutputTemplate = configurationFile.AppSettings.SyslogOutputTemplate,
            ConsoleLogOutputTemplate = configurationFile.AppSettings.ConsoleLogOutputTemplate,
            FileLogOutputTemplate = configurationFile.AppSettings.FileLogOutputTemplate,
            LogFileMaxSizeBytes = configurationFile.AppSettings.LogFileMaxSizeBytes ?? 1073741824,
            LoggingLevel = configurationFile.AppSettings.LoggingLevel ?? "Debug"
        };
        var urls = !string.IsNullOrWhiteSpace(configurationFile.AppSettings.MultifactorApiUrl) ? configurationFile.AppSettings.MultifactorApiUrl :
            throw InvalidConfigurationException.RequiredFor(prop => prop.AppSettings.MultifactorApiUrl, configurationFile.FileName);
        conf.MultifactorApiUrls = ConfigurationValueProcessor.TryParseUrls(urls, out var parsedUrls)
            ? parsedUrls
            : throw new InvalidConfigurationException($"Invalid 'multifactor-api-url': '{urls}'", configurationFile.FileName);
        var endpoint = !string.IsNullOrWhiteSpace(configurationFile.AppSettings.AdapterServerEndpoint) ? configurationFile.AppSettings.AdapterServerEndpoint : throw new InvalidConfigurationException(nameof(conf.AdapterServerEndpoint));

        conf.AdapterServerEndpoint = ConfigurationValueProcessor.TryParseEndpoint(endpoint, out var point)
            ? point
            : throw new InvalidConfigurationException($"Invalid 'adapter-server-endpoint': '{endpoint}'", configurationFile.FileName);
        return conf;
    }
}