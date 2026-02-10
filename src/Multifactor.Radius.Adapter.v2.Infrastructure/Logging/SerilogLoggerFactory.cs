using Elastic.CommonSchema.Serilog;
using Multifactor.Radius.Adapter.v2.Application.Configuration.Models;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configurations.Exceptions;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Compact;
using Serilog.Sinks.Syslog;
using Serilog.Sinks.SystemConsole.Themes;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Logging;

public static class SerilogLoggerFactory
{
    public static LoggerConfiguration CreateLogger(LoggerConfiguration loggerConfiguration, IRootConfiguration rootConfiguration)
    {
        ArgumentNullException.ThrowIfNull(rootConfiguration);

        var levelSwitch = new LoggingLevelSwitch(LogEventLevel.Information);

        loggerConfiguration.MinimumLevel.ControlledBy(levelSwitch)
            .MinimumLevel.Override("Microsoft.Extensions.Http.DefaultHttpClientFactory", LogEventLevel.Warning)
            .Enrich.FromLogContext();

        ConfigureLogging(
            loggerConfiguration,
            rootConfiguration.LoggingFormat,
            rootConfiguration.FileLogOutputTemplate,
            rootConfiguration.LogFileMaxSizeBytes,
            rootConfiguration.ConsoleLogOutputTemplate);
        ConfigureSyslog(loggerConfiguration,
            rootConfiguration.SyslogServer,
            rootConfiguration.SyslogFormat,
            rootConfiguration.SyslogOutputTemplate,
            rootConfiguration.SyslogFacility,
            rootConfiguration.SyslogFramer,
            rootConfiguration.SyslogAppName,
            rootConfiguration.SyslogUseTls
            );
        var level = rootConfiguration.LoggingLevel;
        if (string.IsNullOrWhiteSpace(level))
        {
         throw new InvalidConfigurationException(
             string.Concat("'{prop}' element not found. Config name: '{0}'", "rootConfiguration.ConfigurationName"));
        }

        SetLogLevel(levelSwitch, level);

        return loggerConfiguration;
    }

    private static void ConfigureLogging(
        LoggerConfiguration loggerConfiguration,
        string? loggingFormat,
        string? fileTemplate,
        int? fileSize,
        string? consoleTemplate)
    {
        var adapterPath = Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory);
        var logsPath = $"{adapterPath}{Path.DirectorySeparatorChar}logs{Path.DirectorySeparatorChar}log-.txt";
        var formatter = GetLogFormatter(loggingFormat);
        if (formatter != null)
        {
            loggerConfiguration
                .WriteTo.Console(formatter)
                .WriteTo.File(formatter,
                    logsPath,
                    flushToDiskInterval: TimeSpan.FromSeconds(1),
                    rollingInterval: RollingInterval.Day,
                    fileSizeLimitBytes: fileSize) ;

            if (!string.IsNullOrWhiteSpace(fileTemplate))
            {
                Log.Logger.Warning(
                    "The 'logging-format' parameter cannot be used together with the template. The 'file-log-output-template' parameter will be ignored.");
            }

            return;
        }

        if (!string.IsNullOrWhiteSpace(consoleTemplate))
            loggerConfiguration.WriteTo.Console(outputTemplate: consoleTemplate);
        else
            loggerConfiguration.WriteTo.Console(
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
                theme: AnsiConsoleTheme.Code,
                restrictedToMinimumLevel: LogEventLevel.Information);
        
        if (!string.IsNullOrWhiteSpace(fileTemplate))
        {
            loggerConfiguration.WriteTo.File(
                logsPath,
                flushToDiskInterval: TimeSpan.FromSeconds(1),
                rollingInterval: RollingInterval.Day,
                outputTemplate: fileTemplate);
        }
        else
        {
            loggerConfiguration.WriteTo.File(
                logsPath,
                flushToDiskInterval: TimeSpan.FromSeconds(1),
                rollingInterval: RollingInterval.Day);
        }
    }

    private static void ConfigureSyslog(
        LoggerConfiguration loggerConfiguration,
        string server,
        string format,
        string template,
        string facility,
        string framingType,
        string? appName,
        bool? useTls)
    {
        if (string.IsNullOrWhiteSpace(server))
        {
            return;
        }

        var facilityEnum = Facility.Auth;
        var logFormatEnum = SyslogFormat.RFC5424;
        var framingTypeEnum = FramingType.OCTET_COUNTING;
        if (Enum.TryParse<Facility>(facility, true, out var facilityValue))
        {
            facilityEnum = facilityValue;
        }
        if(Enum.TryParse<SyslogFormat>(format, true, out var syslogFormatValue))
        {
            logFormatEnum =  syslogFormatValue;
        }
        if (Enum.TryParse<FramingType>(framingType, true, out var framingTypeValue))
        {
            framingTypeEnum = framingTypeValue;
        }
        
        var uri = new Uri(server);
        switch (uri.Scheme)
        {
            case "udp":
                loggerConfiguration
                    .WriteTo
                    .UdpSyslog(
                        host: uri.Host,
                        port: uri.Port,
                        appName: appName,
                        format: logFormatEnum,
                        facility: facilityEnum,
                        outputTemplate: template);
                break;
            case "tcp":
                loggerConfiguration
                    .WriteTo
                    .TcpSyslog(
                        host: uri.Host,
                        port: uri.Port,
                        appName: appName,
                        format: logFormatEnum,
                        facility: facilityEnum,
                        outputTemplate: template,
                        framingType: framingTypeEnum,
                        certValidationCallback: (_,_,_,_)=> true,
                        useTls: useTls ?? false);
                break;
            default:
                throw new NotImplementedException($"Unknown scheme {uri.Scheme} for syslog-server {server}. Expected udp or tcp");
        }

    }

    private static void SetLogLevel(LoggingLevelSwitch levelSwitch, string level)
    {
        levelSwitch.MinimumLevel = level switch
        {
            "Verbose" => LogEventLevel.Verbose,
            "Debug" => LogEventLevel.Debug,
            "Info" => LogEventLevel.Information,
            "Warn" => LogEventLevel.Warning,
            "Error" => LogEventLevel.Error,
            _ => levelSwitch.MinimumLevel
        };

        Log.Logger.Information("Logging minimum level: {Level:l}", levelSwitch.MinimumLevel);
    }

    private static ITextFormatter? GetLogFormatter(string? loggingFormat)
    {
        if (string.IsNullOrWhiteSpace(loggingFormat))
            return null;

        if (!Enum.TryParse<SerilogJsonFormatterTypes>(loggingFormat, true, out var formatterType))
            return null;
        
        return formatterType switch
        {
            SerilogJsonFormatterTypes.Json or SerilogJsonFormatterTypes.JsonUtc => new RenderedCompactJsonFormatter(),
            SerilogJsonFormatterTypes.JsonTz => new CustomCompactJsonFormatter("yyyy-MM-dd HH:mm:ss.fff zzz"),
            SerilogJsonFormatterTypes.ElasticCommonSchema => new EcsTextFormatter(),
            _ => null,
        };
    }
}