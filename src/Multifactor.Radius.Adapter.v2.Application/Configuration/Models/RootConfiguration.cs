using System.Net;
using Multifactor.Radius.Adapter.v2.Shared.Attributes;

namespace Multifactor.Radius.Adapter.v2.Application.Configuration.Models;

public class RootConfiguration
{    
    [ConfigParameter("multifactor-api-url")]
    public IReadOnlyList<Uri> MultifactorApiUrls { get; set; }
    [ConfigParameter("multifactor-api-proxy")]
    public string? MultifactorApiProxy { get; set; }
    [ConfigParameter("multifactor-api-timeout")]
    public TimeSpan MultifactorApiTimeout { get; set; }
    [ConfigParameter("adapter-server-endpoint")]
    public IPEndPoint? AdapterServerEndpoint { get; set; }
    [ConfigParameter("logging-level")]
    public string LoggingLevel { get; set; }
    
    [ConfigParameter("logging-format")]
    public string LoggingFormat { get; set; }
    [ConfigParameter("syslog-use-tls")]
    public bool SyslogUseTls { get; set; }
    [ConfigParameter("syslog-server")]
    public string SyslogServer { get; set; }
    [ConfigParameter("syslog-format")]
    public string SyslogFormat { get; set; }
    [ConfigParameter("syslog-facility")]
    public string SyslogFacility { get; set; }
    [ConfigParameter("syslog-app-name", "multifactor-radius")]
    public string SyslogAppName { get; set; }
    [ConfigParameter("syslog-framer")]
    public string SyslogFramer { get; set; }
    [ConfigParameter("syslog-output-template")]
    public string SyslogOutputTemplate { get; set; }
    
    [ConfigParameter("console-log-output-template")]
    public string ConsoleLogOutputTemplate { get; set; }
    [ConfigParameter("file-log-output-template")]
    public string FileLogOutputTemplate { get; set; }
    [ConfigParameter("log-file-max-size-bytes", 1073741824)]
    public int LogFileMaxSizeBytes { get; set; }
}