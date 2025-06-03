using Elastic.CommonSchema.Serilog;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configuration.Exceptions;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configuration.RadiusAdapter;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Compact;

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
            rootConfiguration.AppSettings.FileLogOutputTemplate,
            rootConfiguration.AppSettings.ConsoleLogOutputTemplate);

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
                    rollingInterval: RollingInterval.Day);

            if (!string.IsNullOrEmpty(fileTemplate))
            {
                Log.Logger.Warning(
                    "The {LoggingFormat:l} parameter cannot be used together with the template. The {FileLogOutputTemplate:l} parameter will be ignored.",
                    RadiusAdapterConfigurationDescription.Property(x => x.AppSettings.LoggingFormat),
                    RadiusAdapterConfigurationDescription.Property(x => x.AppSettings.FileLogOutputTemplate));
            }

            return;
        }

        if (!string.IsNullOrEmpty(consoleTemplate))
        {
            loggerConfiguration.WriteTo.Console(outputTemplate: consoleTemplate);
        }
        else
        {
            loggerConfiguration.WriteTo.Console();
        }

        if (fileTemplate != null)
        {
            loggerConfiguration.WriteTo.File(
                logsPath,
                rollingInterval: RollingInterval.Day,
                outputTemplate: fileTemplate);
        }
        else
        {
            loggerConfiguration.WriteTo.File(
                logsPath,
                rollingInterval: RollingInterval.Day);
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
        if (string.IsNullOrEmpty(loggingFormat))
        {
            return null;
        }

        if (!Enum.TryParse<SerilogJsonFormatterTypes>(loggingFormat, true, out var formatterType))
        {
            return null;
        }

        return formatterType switch
        {
            SerilogJsonFormatterTypes.Json or SerilogJsonFormatterTypes.JsonUtc => new RenderedCompactJsonFormatter(),
            SerilogJsonFormatterTypes.JsonTz => new CustomCompactJsonFormatter("yyyy-MM-dd HH:mm:ss.fff zzz"),
            SerilogJsonFormatterTypes.ElasticCommonSchema => new EcsTextFormatter(),
            _ => null,
        };
    }
}