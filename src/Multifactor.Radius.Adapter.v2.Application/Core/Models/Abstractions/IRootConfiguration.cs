using System.Net;

namespace Multifactor.Radius.Adapter.v2.Application.Core.Models.Abstractions;

public interface IRootConfiguration
{
    IReadOnlyList<Uri> MultifactorApiUrls { get; }
    string? MultifactorApiProxy { get; }
    TimeSpan MultifactorApiTimeout { get; }
    IPEndPoint AdapterServerEndpoint { get; }
    string LoggingLevel { get; }
    string? LoggingFormat { get; }
    bool SyslogUseTls { get; }
    string? SyslogServer { get; }
    string? SyslogFormat { get; }
    string? SyslogFacility { get; }
    string SyslogAppName { get; }
    string? SyslogFramer { get; }
    string? SyslogOutputTemplate { get; }

    string? ConsoleLogOutputTemplate { get; }
    string? FileLogOutputTemplate { get; }
    int LogFileMaxSizeBytes { get; }
}