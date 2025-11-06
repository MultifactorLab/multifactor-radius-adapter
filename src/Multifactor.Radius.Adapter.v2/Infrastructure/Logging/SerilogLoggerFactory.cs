using Elastic.CommonSchema.Serilog;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configuration.Exceptions;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configuration.RadiusAdapter;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Compact;
using Serilog.Sinks.Syslog;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Logging;

public static class SerilogLoggerFactory
{
    public static ILogger CreateLogger(RadiusAdapterConfiguration rootConfiguration)
    {
        ArgumentNullException.ThrowIfNull(rootConfiguration);

        var levelSwitch = new LoggingLevelSwitch(LogEventLevel.Information);

        var loggerConfiguration = new LoggerConfiguration()
            .MinimumLevel.ControlledBy(levelSwitch)
            .MinimumLevel.Override("Microsoft.Extensions.Http.DefaultHttpClientFactory", LogEventLevel.Warning)
            .Enrich.FromLogContext();

        ConfigureLogging(
            loggerConfiguration,
            rootConfiguration.AppSettings.LoggingFormat,
            rootConfiguration.AppSettings.SyslogOutputTemplate,
            rootConfiguration.AppSettings.ConsoleLogOutputTemplate);
        ConfigureSyslog(loggerConfiguration,
            rootConfiguration.AppSettings.SyslogServer,
            rootConfiguration.AppSettings.SyslogFormat,
            rootConfiguration.AppSettings.SyslogOutputTemplate,
            rootConfiguration.AppSettings.SyslogFacility,
            rootConfiguration.AppSettings.SyslogFramer,
            rootConfiguration.AppSettings.SyslogAppName,
            rootConfiguration.AppSettings.SyslogUseTls
            );
        
        var level = rootConfiguration.AppSettings.LoggingLevel;
        if (string.IsNullOrWhiteSpace(level))
        {
            throw InvalidConfigurationException.For(x => x.AppSettings.LoggingLevel,
                "'{prop}' element not found. Config name: '{0}'",
                RadiusAdapterConfigurationFile.ConfigName);
        }

        SetLogLevel(levelSwitch, level);
        var logger = loggerConfiguration.CreateLogger();

        return logger;
    }

    private static void ConfigureLogging(
        LoggerConfiguration loggerConfiguration,
        string? loggingFormat,
        string? fileTemplate,
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
                    rollingInterval: RollingInterval.Day) ;

            if (!string.IsNullOrWhiteSpace(fileTemplate))
            {
                Log.Logger.Warning(
                    "The {LoggingFormat:l} parameter cannot be used together with the template. The {FileLogOutputTemplate:l} parameter will be ignored.",
                    RadiusAdapterConfigurationDescription.Property(x => x.AppSettings.LoggingFormat),
                    RadiusAdapterConfigurationDescription.Property(x => x.AppSettings.FileLogOutputTemplate));
            }

            return;
        }

        if (!string.IsNullOrWhiteSpace(consoleTemplate))
            loggerConfiguration.WriteTo.Console(outputTemplate: consoleTemplate);
        else
            loggerConfiguration.WriteTo.Console();

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
        switch (level)
        {
            case "Verbose":
                levelSwitch.MinimumLevel = LogEventLevel.Verbose;
                break;
            case "Debug":
                levelSwitch.MinimumLevel = LogEventLevel.Debug;
                break;
            case "Info":
                levelSwitch.MinimumLevel = LogEventLevel.Information;
                break;
            case "Warn":
                levelSwitch.MinimumLevel = LogEventLevel.Warning;
                break;
            case "Error":
                levelSwitch.MinimumLevel = LogEventLevel.Error;
                break;
        }

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