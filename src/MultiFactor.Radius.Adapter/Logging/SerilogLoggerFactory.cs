using System;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using MultiFactor.Radius.Adapter.Configuration;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting.Compact;
using Serilog.Formatting;
using MultiFactor.Radius.Adapter.Core;
using MultiFactor.Radius.Adapter.Core.Exceptions;
using MultiFactor.Radius.Adapter.Configuration.Core;

namespace MultiFactor.Radius.Adapter.Logging
{
    public class SerilogLoggerFactory
    {
        private readonly Lazy<System.Configuration.Configuration> _rootConfig;

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
            _rootConfig = new Lazy<System.Configuration.Configuration>(() => _rootConfigurationProvider.GetRootConfiguration());
        }

        public ILogger CreateLogger()
        {
            var levelSwitch = new LoggingLevelSwitch(LogEventLevel.Information);

            var loggerConfiguration = new LoggerConfiguration()
                .MinimumLevel.ControlledBy(levelSwitch)
                .Enrich.FromLogContext();

            ConfigureLogging(_variables.AppPath, loggerConfiguration);

            var config = _rootConfigurationProvider.GetRootConfiguration();
            var level = config.AppSettings.Settings[Literals.Configuration.LoggingLevel]?.Value;
            if (string.IsNullOrWhiteSpace(level))
            {
                throw new InvalidConfigurationException($"'{Literals.Configuration.LoggingLevel}' element not found");
            }

            SetLogLevel(level, levelSwitch);
            var logger = loggerConfiguration.CreateLogger();

            return logger;
        }

        private void ConfigureLogging(string path, LoggerConfiguration loggerConfiguration)
        {
            var formatter = GetLogFormatter();
            if (formatter != null)
            {
                loggerConfiguration
                    .WriteTo.Console(formatter)
                    .WriteTo.File(formatter, $"{path}logs{Path.DirectorySeparatorChar}log-.txt", rollingInterval: RollingInterval.Day);
            }
            else
            {
                var consoleTemplate = GetStringSettingOrNull(Core.Literals.Configuration.ConsoleLogOutputTemplate);
                if (consoleTemplate != null)
                {
                    loggerConfiguration.WriteTo.Console(outputTemplate: consoleTemplate);
                }
                else
                {
                    loggerConfiguration.WriteTo.Console();
                }

                var fileTemplate = GetStringSettingOrNull(Core.Literals.Configuration.FileLogOutputTemplate);
                if (fileTemplate != null)
                {
                    loggerConfiguration.WriteTo.File(
                        $"{path}logs{Path.DirectorySeparatorChar}log-.txt",
                        rollingInterval: RollingInterval.Day,
                        outputTemplate: fileTemplate);
                }
                else
                {
                    loggerConfiguration.WriteTo.File(
                        $"{path}logs{Path.DirectorySeparatorChar}log-.txt",
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
            switch (format?.ToLower())
            {
                case "json":
                    return new RenderedCompactJsonFormatter();
                default:
                    return null;
            }
        }

        private string GetStringSettingOrNull(string key)
        {
            var config = _rootConfigurationProvider.GetRootConfiguration();
            var value = config.AppSettings.Settings[key]?.Value;
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }
    }
}
