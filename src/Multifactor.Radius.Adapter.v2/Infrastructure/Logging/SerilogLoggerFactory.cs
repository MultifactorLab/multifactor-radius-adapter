using Elastic.CommonSchema.Serilog;
using Multifactor.Radius.Adapter.v2.Domain.RadiusAdapter;
using Multifactor.Radius.Adapter.v2.Domain.RadiusAdapter.Sections;
using Multifactor.Radius.Adapter.v2.Infrastructure.Exceptions;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Compact;
using Serilog.Sinks.Syslog;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Logging;

public static class SerilogLoggerFactory
{
    public static ILogger CreateLogger(RadiusAdapterConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var levelSwitch = CreateLevelSwitch(configuration);
        var loggerConfig = CreateLoggerConfiguration(levelSwitch);

        ConfigureLogging(loggerConfig, configuration.AppSettings);
        ConfigureSyslog(loggerConfig, configuration.AppSettings);

        return loggerConfig.CreateLogger();
    }

    private static LoggingLevelSwitch CreateLevelSwitch(RadiusAdapterConfiguration configuration)
    {
        var levelSwitch = new LoggingLevelSwitch(LogEventLevel.Information);
        var level = configuration.AppSettings.LoggingLevel;

        if (string.IsNullOrWhiteSpace(level))
        {
            throw new InvalidConfigurationException(
                $"LoggingLevel is required. Config: {configuration.AppSettings.LoggingLevel}");
        }

        SetLogLevel(levelSwitch, level);
        return levelSwitch;
    }

    private static LoggerConfiguration CreateLoggerConfiguration(LoggingLevelSwitch levelSwitch)
    {
        return new LoggerConfiguration()
            .MinimumLevel.ControlledBy(levelSwitch)
            .MinimumLevel.Override("Microsoft.Extensions.Http.DefaultHttpClientFactory", LogEventLevel.Warning)
            .Enrich.FromLogContext();
    }

    private static void ConfigureLogging(LoggerConfiguration loggerConfig, AppSettingsSection settings)
    {
        var logsPath = GetLogsPath();
        var formatter = GetLogFormatter(settings.LoggingFormat);

        if (formatter != null)
        {
            loggerConfig
                .WriteTo.Console(formatter)
                .WriteTo.File(formatter, logsPath,
                    flushToDiskInterval: TimeSpan.FromSeconds(1),
                    rollingInterval: RollingInterval.Day);

            if (!string.IsNullOrWhiteSpace(settings.FileLogOutputTemplate))
            {
                Log.Logger.Warning(
                    "LoggingFormat cannot be used with FileLogOutputTemplate. FileLogOutputTemplate will be ignored.");
            }
        }
        else
        {
            ConfigureConsole(loggerConfig, settings.ConsoleLogOutputTemplate);
            ConfigureFile(loggerConfig, logsPath, settings.FileLogOutputTemplate);
        }
    }

    private static string GetLogsPath()
    {
        var adapterPath = AppDomain.CurrentDomain.BaseDirectory;
        return Path.Combine(adapterPath, "logs", "log-.txt");
    }

    private static void ConfigureConsole(LoggerConfiguration loggerConfig, string? template)
    {
        if (!string.IsNullOrWhiteSpace(template))
            loggerConfig.WriteTo.Console(outputTemplate: template);
        else
            loggerConfig.WriteTo.Console();
    }

    private static void ConfigureFile(LoggerConfiguration loggerConfig, string path, string? template)
    {
        if (!string.IsNullOrWhiteSpace(template))
        {
            loggerConfig.WriteTo.File(path,
                flushToDiskInterval: TimeSpan.FromSeconds(1),
                rollingInterval: RollingInterval.Day,
                outputTemplate: template);
        }
        else
        {
            loggerConfig.WriteTo.File(path,
                flushToDiskInterval: TimeSpan.FromSeconds(1),
                rollingInterval: RollingInterval.Day);
        }
    }

    private static void ConfigureSyslog(LoggerConfiguration loggerConfig, AppSettingsSection settings)
    {
        if (string.IsNullOrWhiteSpace(settings.SyslogServer))
            return;

        var syslogConfig = ParseSyslogConfig(settings);
        var uri = new Uri(settings.SyslogServer);

        switch (uri.Scheme.ToLowerInvariant())
        {
            case "udp":
                loggerConfig.WriteTo.UdpSyslog(
                    host: uri.Host,
                    port: uri.Port,
                    appName: syslogConfig.AppName,
                    format: syslogConfig.Format,
                    facility: syslogConfig.Facility,
                    outputTemplate: settings.SyslogOutputTemplate);
                break;

            case "tcp":
                loggerConfig.WriteTo.TcpSyslog(
                    host: uri.Host,
                    port: uri.Port,
                    appName: syslogConfig.AppName,
                    format: syslogConfig.Format,
                    facility: syslogConfig.Facility,
                    outputTemplate: settings.SyslogOutputTemplate,
                    framingType: syslogConfig.FramingType,
                    certValidationCallback: (_, _, _, _) => true,
                    useTls: syslogConfig.UseTls ?? false);
                break;

            default:
                throw new NotImplementedException(
                    $"Unknown scheme {uri.Scheme} for syslog. Expected udp or tcp");
        }
    }

    private static SyslogConfig ParseSyslogConfig(AppSettingsSection settings)
    {
        Enum.TryParse<Facility>(settings.SyslogFacility, true, out var facility);
        Enum.TryParse<SyslogFormat>(settings.SyslogFormat, true, out var format);
        Enum.TryParse<FramingType>(settings.SyslogFramer, true, out var framingType);

        return new SyslogConfig
        {
            Facility = facility,
            Format = format,
            FramingType = framingType,
            AppName = settings.SyslogAppName,
            UseTls = settings.SyslogUseTls
        };
    }

    private static void SetLogLevel(LoggingLevelSwitch levelSwitch, string level)
    {
        levelSwitch.MinimumLevel = level.ToLowerInvariant() switch
        {
            "verbose" => LogEventLevel.Verbose,
            "debug" => LogEventLevel.Debug,
            "info" => LogEventLevel.Information,
            "warn" => LogEventLevel.Warning,
            "error" => LogEventLevel.Error,
            _ => LogEventLevel.Information
        };

        Log.Logger.Information("Logging minimum level: {Level}", levelSwitch.MinimumLevel);
    }

    private static ITextFormatter? GetLogFormatter(string? format)
    {
        if (string.IsNullOrWhiteSpace(format))
            return null;

        if (!Enum.TryParse<SerilogJsonFormatterTypes>(format, true, out var formatterType))
            return null;

        return formatterType switch
        {
            SerilogJsonFormatterTypes.Json or SerilogJsonFormatterTypes.JsonUtc 
                => new RenderedCompactJsonFormatter(),
            SerilogJsonFormatterTypes.JsonTz 
                => new CustomCompactJsonFormatter("yyyy-MM-dd HH:mm:ss.fff zzz"),
            SerilogJsonFormatterTypes.ElasticCommonSchema 
                => new EcsTextFormatter(),
            _ => null
        };
    }

    private class SyslogConfig
    {
        public Facility Facility { get; set; } = Facility.Auth;
        public SyslogFormat Format { get; set; } = SyslogFormat.RFC5424;
        public FramingType FramingType { get; set; } = FramingType.OCTET_COUNTING;
        public string? AppName { get; set; }
        public bool? UseTls { get; set; }
    }
}