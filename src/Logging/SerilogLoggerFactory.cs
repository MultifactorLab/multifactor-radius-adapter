using System;
using System.Configuration;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using MultiFactor.Radius.Adapter.Configuration;
using MultiFactor.Radius.Adapter.Configuration.ConfigurationLoading;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting.Compact;
using Serilog.Formatting;
using MultiFactor.Radius.Adapter.Core;

namespace MultiFactor.Radius.Adapter.Logging
{
    public class SerilogLoggerFactory
    {
        private readonly ApplicationVariables _variables;
        private readonly IAppConfigurationProvider _appConfigurationProvider;

        public SerilogLoggerFactory(ApplicationVariables variables, IAppConfigurationProvider appConfigurationProvider)
        {
            if (variables is null)
            {
                throw new ArgumentNullException(nameof(variables));
            }
            _variables = variables;
            _appConfigurationProvider = appConfigurationProvider ?? throw new ArgumentNullException(nameof(appConfigurationProvider));
        }

        public ILogger CreateLogger()
        {
            var levelSwitch = new LoggingLevelSwitch(LogEventLevel.Information);

            var loggerConfiguration = new LoggerConfiguration()
                .MinimumLevel.ControlledBy(levelSwitch)
                .Enrich.FromLogContext();

            ConfigureLogging(_variables.AppPath, loggerConfiguration);

            var config = _appConfigurationProvider.GetRootConfiguration();

            var appSettingsSection = config.GetSection("appSettings");
            var appSettings = appSettingsSection as AppSettingsSection;

            var level = appSettings.Settings["logging-level"]?.Value;
            if (string.IsNullOrWhiteSpace(level))
            {
                throw new Exception("Configuration error: 'logging-level' element not found");
            }

            SetLogLevel(level, levelSwitch);
            var logger = loggerConfiguration.CreateLogger();

            return logger;
        }

        private static void ConfigureLogging(string path, LoggerConfiguration loggerConfiguration)
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

        private static ITextFormatter GetLogFormatter()
        {
            var format = ConfigurationManager.AppSettings?["logging-format"];
            switch (format?.ToLower())
            {
                case "json":
                    return new RenderedCompactJsonFormatter();
                default:
                    return null;
            }
        }

        private static string GetStringSettingOrNull(string key)
        {
            var value = ConfigurationManager.AppSettings[key];
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }
    }
}
