using System;
using System.IO;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MultiFactor.Radius.Adapter.Core;
using MultiFactor.Radius.Adapter.Server;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace MultiFactor.Radius.Adapter
{
    public class Program
    {
        public static void Main(string[] args)
        {
            IHost host = null;
            
            try
            {
                host = Host
                    .CreateDefaultBuilder(args)
                    .ConfigureServices(services => ConfigureServices(services))
                    .Build();

                host.Run();
            }
            catch(Exception ex)
            {
                var errorMessage = FlattenException(ex);

                if (Log.Logger != null)
                {
                    Log.Logger.Error($"Unable to start: {errorMessage}");
                }
                else
                {
                    Console.WriteLine($"Unable to start: {errorMessage}");
                }

                if (host != null)
                {
                    host.StopAsync();
                }
            }

        }

        private static void ConfigureServices(IServiceCollection services)
        {
            var path = Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory) + Path.DirectorySeparatorChar;

            //create logging
            var levelSwitch = new LoggingLevelSwitch(LogEventLevel.Information);
            var loggerConfiguration = new LoggerConfiguration()
                .MinimumLevel.ControlledBy(levelSwitch)
                .WriteTo.Console(LogEventLevel.Debug)
                .WriteTo.File($"{path}log{Path.DirectorySeparatorChar}log-.txt", rollingInterval: RollingInterval.Day);

            Log.Logger = loggerConfiguration.CreateLogger();


            //load radius attributes dictionary
            var dictionaryPath = path + "content" + Path.DirectorySeparatorChar + "radius.dictionary";
            var dictionary = new RadiusDictionary(dictionaryPath, Log.Logger);

            //init configuration
            var configuration = Configuration.Load(dictionary);

            SetLogLevel(configuration.LogLevel, levelSwitch);

            services.AddSingleton(Log.Logger);
            services.AddSingleton(configuration);
            services.AddSingleton<IRadiusDictionary>(dictionary);
            services.AddSingleton<IRadiusPacketParser, RadiusPacketParser>();
            services.AddSingleton<RadiusServer>();

            services.AddHostedService<ServerHost>();
        }

        private static void SetLogLevel(string level, LoggingLevelSwitch levelSwitch)
        {
            switch (level)
            {
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

        private static string FlattenException(Exception exception)
        {
            var stringBuilder = new StringBuilder();

            var counter = 0;

            while (exception != null)
            {
                if (counter++ > 0)
                {
                    var prefix = new string('-', counter) + ">\t";
                    stringBuilder.Append(prefix);
                }

                stringBuilder.AppendLine(exception.Message);
                exception = exception.InnerException;
            }

            return stringBuilder.ToString();
        }
    }
}
