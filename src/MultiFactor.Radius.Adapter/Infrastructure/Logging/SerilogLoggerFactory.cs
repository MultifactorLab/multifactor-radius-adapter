using Elastic.CommonSchema.Serilog;
using MultiFactor.Radius.Adapter.Core;
using MultiFactor.Radius.Adapter.Core.Serialization;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration.Exceptions;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Compact;
using System;
using System.IO;

namespace MultiFactor.Radius.Adapter.Infrastructure.Logging
{
    public class SerilogLoggerFactory
    {
        private readonly IRootConfigurationProvider _rootConfigurationProvider;

        public SerilogLoggerFactory(ApplicationVariables variables, IRootConfigurationProvider rootConfigurationProvider)
        {
            if (variables is null)
            {
                throw new ArgumentNullException(nameof(variables));
            }
            _rootConfigurationProvider = rootConfigurationProvider ?? throw new ArgumentNullException(nameof(rootConfigurationProvider));
        }

        public ILogger CreateLogger()
        {
            var levelSwitch = new LoggingLevelSwitch(LogEventLevel.Information);

            var loggerConfiguration = new LoggerConfiguration()
                .MinimumLevel.ControlledBy(levelSwitch)
                .MinimumLevel.Override("Microsoft.Extensions.Http.DefaultHttpClientFactory", LogEventLevel.Warning)
                .Enrich.FromLogContext();

            ConfigureLogging(loggerConfiguration);

            var config = _rootConfigurationProvider.GetRootConfiguration();
            var level = config.AppSettings.LoggingLevel;
            if (string.IsNullOrWhiteSpace(level))
            {
                throw new InvalidConfigurationException($"'{LoggingLevel}' element not found");
            }

            SetLogLevel(level, levelSwitch);
            var logger = loggerConfiguration.CreateLogger();

            return logger;
        }

        private void ConfigureLogging(LoggerConfiguration loggerConfiguration)
        {
            var path = Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory);
            var fileTemplate = _rootConfigurationProvider.GetRootConfiguration().AppSettings.FileLogOutputTemplate;
            var consoleTemplate = _rootConfigurationProvider.GetRootConfiguration().AppSettings.ConsoleLogOutputTemplate;

            var formatter = GetLogFormatter();
            if (formatter != null)
            {
                loggerConfiguration
                    .WriteTo.Console(formatter)
                    .WriteTo.File(formatter,
                    $"{path}{Path.DirectorySeparatorChar}logs{Path.DirectorySeparatorChar}log-.txt",
                    rollingInterval: RollingInterval.Day);

                if (!string.IsNullOrEmpty(fileTemplate))
                {
                    Log.Logger.Warning($"The {LoggingFormat} parameter cannot be used together with the template. The {FileLogOutputTemplate} parameter will be ignored.");
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
                    $"{path}{Path.DirectorySeparatorChar}logs{Path.DirectorySeparatorChar}log-.txt",
                    rollingInterval: RollingInterval.Day,
                    outputTemplate: fileTemplate);
            }
            else
            {
                loggerConfiguration.WriteTo.File(
                    $"{path}{Path.DirectorySeparatorChar}logs{Path.DirectorySeparatorChar}log-.txt",
                    rollingInterval: RollingInterval.Day);
            }
            
        }

        private static void SetLogLevel(string level, LoggingLevelSwitch levelSwitch)
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
            Log.Logger.Information($"Logging level: {levelSwitch.MinimumLevel}");
        }

        private ITextFormatter GetLogFormatter()
        {
            var root = _rootConfigurationProvider.GetRootConfiguration();

            var format = root.AppSettings.LoggingFormat;
            if (string.IsNullOrEmpty(format))
            {
                return null;
            }

            if (!Enum.TryParse<SerilogJsonFormatterTypes>(format, true, out var formatterType))
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
}
