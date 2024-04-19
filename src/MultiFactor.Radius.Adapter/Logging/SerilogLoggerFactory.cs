using Elastic.CommonSchema.Serilog;
using MultiFactor.Radius.Adapter.Configuration.Core;
using MultiFactor.Radius.Adapter.Core;
using MultiFactor.Radius.Adapter.Core.Exceptions;
using MultiFactor.Radius.Adapter.Core.Serialization;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Compact;
using System;
using System.IO;
using static MultiFactor.Radius.Adapter.Core.Literals.Configuration;

namespace MultiFactor.Radius.Adapter.Logging
{
    public class SerilogLoggerFactory
    {
        private readonly ApplicationVariables _variables;
        private readonly IRootConfigurationProvider _rootConfigurationProvider;

        public SerilogLoggerFactory(ApplicationVariables variables, IRootConfigurationProvider rootConfigurationProvider)
        {
            if (variables is null)
            {
                throw new ArgumentNullException(nameof(variables));
            }
            _variables = variables;
            _rootConfigurationProvider = rootConfigurationProvider ?? throw new ArgumentNullException(nameof(rootConfigurationProvider));
        }

        public ILogger CreateLogger()
        {
            var levelSwitch = new LoggingLevelSwitch(LogEventLevel.Information);

            var loggerConfiguration = new LoggerConfiguration()
                .MinimumLevel.ControlledBy(levelSwitch)
                .MinimumLevel.Override("Microsoft.Extensions.Http.DefaultHttpClientFactory", LogEventLevel.Warning)
                .Enrich.FromLogContext();

            ConfigureLogging(_variables.AppPath, loggerConfiguration);

            var config = _rootConfigurationProvider.GetRootConfiguration();
            var level = config.AppSettings.Settings[LoggingLevel]?.Value;
            if (string.IsNullOrWhiteSpace(level))
            {
                throw new InvalidConfigurationException($"'{LoggingLevel}' element not found");
            }

            SetLogLevel(level, levelSwitch);
            var logger = loggerConfiguration.CreateLogger();

            return logger;
        }

        private void ConfigureLogging(string path, LoggerConfiguration loggerConfiguration)
        {
            var formatter = GetLogFormatter();
            var fileTemplate = GetStringSettingOrNull(FileLogOutputTemplate);
            if (formatter != null)
            {
                loggerConfiguration
                    .WriteTo.Console(formatter)
                    .WriteTo.File(formatter,
                    $"{path}{Path.DirectorySeparatorChar}logs{Path.DirectorySeparatorChar}log-.txt",
                    rollingInterval: RollingInterval.Day);

                if (fileTemplate != null)
                {
                    Log.Logger.Warning($"The {LoggingFormat} parameter cannot be used together with the template. The {FileLogOutputTemplate} parameter will be ignored.");
                }
            }
            else
            {
                var consoleTemplate = GetStringSettingOrNull(ConsoleLogOutputTemplate);
                if (consoleTemplate != null)
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

            var format = root.AppSettings.Settings[Literals.Configuration.LoggingFormat]?.Value;
            var parseResult = Enum.TryParse<SerilogJsonFormatterTypes>(format, true, out var formatterType);
            if (!parseResult) return null;

            return formatterType switch
            {
                SerilogJsonFormatterTypes.Json or SerilogJsonFormatterTypes.JsonUtc => new RenderedCompactJsonFormatter(),
                SerilogJsonFormatterTypes.JsonTz => new CustomCompactJsonFormatter("yyyy-MM-dd HH:mm:ss.fff zzz"),
                SerilogJsonFormatterTypes.ElasticCommonSchema => new EcsTextFormatter(),
                _ => null,
            };
        }

        private string GetStringSettingOrNull(string key)
        {
            var config = _rootConfigurationProvider.GetRootConfiguration();
            var value = config.AppSettings.Settings[key]?.Value;
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }
    }
}
