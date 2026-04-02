using System.Runtime.InteropServices.JavaScript;
using Serilog;
using Serilog.Core;
using Serilog.Debugging;
using Serilog.Events;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Logging;

public static class StartupLogger
{
    private const string LogDirectory = "logs";
    private const string StartupLogFile = "startup.log";
    private const long FileSizeLimitBytes = 1024 * 1024 * 20;
    private const string FileLogTemplate = "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}|{Level:u3}|{SourceContext:l}] {Message:lj}{NewLine}{Exception}{Properties}{NewLine}";
    private const string ConsoleLogTemplate = "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff} {Level:u3} {SourceContext:l}] {Message:lj} {Exception}{NewLine}";

    private static readonly Lazy<Logger> Logger = new(() =>
    {
        SelfLog.Enable(Console.WriteLine);

        var baseDir = Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory);
        var dir = Path.Combine(baseDir!, LogDirectory);
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var path = Path.Combine(dir, StartupLogFile);
        var loggerConfig = new LoggerConfiguration()
            .WriteTo.File(path: path,
                LogEventLevel.Verbose,
                FileLogTemplate,
                fileSizeLimitBytes: FileSizeLimitBytes,
                rollOnFileSizeLimit: true)
            .WriteTo.Console(LogEventLevel.Verbose, ConsoleLogTemplate)
            .Enrich.FromLogContext();

        return loggerConfig.CreateLogger();
    });

    /// <inheritdoc cref="Logger.Verbose"/>
    public static void Verbose(string message, params object?[] values) => Logger.Value.Verbose(message, values);

    /// <inheritdoc cref="System.Diagnostics.Debug"/>
    public static void Debug(string message, params object?[] values) => Logger.Value.Debug(message, values);

    /// <inheritdoc cref="Microsoft.VisualBasic.Information"/>
    public static void Information(string message, params object?[] values) => Logger.Value.Information(message, values);
    public static void Warning(string message, params object?[] values) => Logger.Value.Warning(message, values);

    /// <inheritdoc cref="JSType.Error"/>
    public static void Error(string message, params object?[] values) => Logger.Value.Error(message, values);

    /// <inheritdoc cref="JSType.Error"/>
    public static void Error(Exception ex, string message, params object?[] values) => Logger.Value.Error(ex, message, values);
}