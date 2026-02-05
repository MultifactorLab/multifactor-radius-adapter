using System.Net;

namespace Multifactor.Radius.Adapter.v2.Application.Configuration.Models;

public interface IRootConfiguration
{
    
    IReadOnlyList<Uri> MultifactorApiUrls { get; set; }
    string? MultifactorApiProxy { get; set; }
    TimeSpan MultifactorApiTimeout { get; set; }
    IPEndPoint? AdapterServerEndpoint { get; set; }
    string LoggingLevel { get; set; }
    string? LoggingFormat { get; set; }
    bool SyslogUseTls { get; set; }
    string? SyslogServer { get; set; }
    string? SyslogFormat { get; set; }
    string? SyslogFacility { get; set; }
    string SyslogAppName { get; set; }
    string? SyslogFramer { get; set; }
    string? SyslogOutputTemplate { get; set; }

    string? ConsoleLogOutputTemplate { get; set; }
    string? FileLogOutputTemplate { get; set; }
    int LogFileMaxSizeBytes { get; set; }
}