using Serilog;
using Serilog.Debugging;
using Serilog.Events;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Logging;
public static class StartupLogger
{
    private const string LogDirectory = "logs";
    private const string StartupLogFile = "startup.log";
    private const long MaxFileSizeBytes = 20 * 1024 * 1024;
    private const string LogTemplate = "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}|{Level:u3}|{SourceContext:l}] {Message:lj}{NewLine}{Exception}";

    private static readonly Lazy<ILogger> Logger = new(CreateLogger);

    public static void Verbose(string message, params object[] values) => 
        Logger.Value.Verbose(message, values);

    public static void Debug(string message, params object[] values) => 
        Logger.Value.Debug(message, values);

    public static void Information(string message, params object[] values) => 
        Logger.Value.Information(message, values);

    public static void Error(string message, params object[] values) => 
        Logger.Value.Error(message, values);

    public static void Error(Exception ex, string message, params object[] values) => 
        Logger.Value.Error(ex, message, values);

    private static ILogger CreateLogger()
    {
        EnableSerilogSelfLog();
        
        var logDirectory = GetLogDirectory();
        var logFilePath = Path.Combine(logDirectory, StartupLogFile);
        
        var loggerConfig = new LoggerConfiguration()
            .WriteTo.File(
                logFilePath,
                LogEventLevel.Verbose,
                LogTemplate,
                fileSizeLimitBytes: MaxFileSizeBytes,
                rollOnFileSizeLimit: true)
            .WriteTo.Console(LogEventLevel.Verbose, LogTemplate)
            .Enrich.FromLogContext();

        return loggerConfig.CreateLogger();
    }

    private static void EnableSerilogSelfLog()
    {
        SelfLog.Enable(Console.WriteLine);
    }

    private static string GetLogDirectory()
    {
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var logDir = Path.Combine(baseDir, LogDirectory);
        
        if (!Directory.Exists(logDir))
            Directory.CreateDirectory(logDir);
            
        return logDir;
    }
}