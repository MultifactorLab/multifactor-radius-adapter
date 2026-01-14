using System.Net;

namespace Multifactor.Radius.Adapter.v2.Application.Configuration.Models;

public class RootConfiguration
{
    public required IReadOnlyList<Uri> MultifactorApiUrls { get; set; }
    public string? MultifactorApiProxy { get; set; }
    public TimeSpan MultifactorApiTimeout { get; set; }
    public required IPEndPoint? AdapterServerEndpoint { get; set; }
    
    public string LoggingFormat { get; set; } = string.Empty;
    public bool SyslogUseTls { get; set; } = false;
    public string SyslogServer { get; set; } = string.Empty;
    public string SyslogFormat { get; set; } = string.Empty;
    public string SyslogFacility { get; set; } = string.Empty;
    public string SyslogAppName { get; set; } = "multifactor-radius";
    public string SyslogFramer { get; set; } = string.Empty;
    public string SyslogOutputTemplate { get; set; } = string.Empty;
    
    public string ConsoleLogOutputTemplate { get; set; } = string.Empty;
    public string FileLogOutputTemplate { get; set; } = string.Empty;
    public int LogFileMaxSizeBytes { get; set; } = 1073741824;
}