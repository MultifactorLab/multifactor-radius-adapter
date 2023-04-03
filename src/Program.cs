using System;
using System.Configuration;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MultiFactor.Radius.Adapter.Configuration;
using MultiFactor.Radius.Adapter.Configuration.ConfigurationLoading;
using MultiFactor.Radius.Adapter.Configuration.Core;
using MultiFactor.Radius.Adapter.Core;
using MultiFactor.Radius.Adapter.Core.Radius;
using MultiFactor.Radius.Adapter.Core.Radius.Attributes;
using MultiFactor.Radius.Adapter.Logging;
using MultiFactor.Radius.Adapter.Server;
using MultiFactor.Radius.Adapter.Server.FirstAuthFactorProcessing;
using MultiFactor.Radius.Adapter.Services;
using MultiFactor.Radius.Adapter.Services.BindIdentityFormatting;
using MultiFactor.Radius.Adapter.Services.Ldap;
using MultiFactor.Radius.Adapter.Services.Ldap.Connection;
using MultiFactor.Radius.Adapter.Services.Ldap.MembershipVerification;
using MultiFactor.Radius.Adapter.Services.Ldap.UserGroupsGetters;
using MultiFactor.Radius.Adapter.Services.MultiFactorApi;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Compact;
using static MultiFactor.Radius.Adapter.Core.Literals;

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
                    .ConfigureServices(ConfigureServices)
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
            services.AddSingleton(prov => ApplicationVariablesFactory.Create());
            services.AddSingleton<IAppConfigurationProvider, DefaultAppConfigurationProvider>();

            services.AddSingleton<SerilogLoggerFactory>();
            services.AddSingleton(prov => 
            {
                var logger = prov.GetRequiredService<SerilogLoggerFactory>().CreateLogger();
                Log.Logger = logger;
                return logger;
            });

            services.AddSingleton<IRadiusDictionary, RadiusDictionary>();

            services.AddSingleton<ServiceConfigurationFactory>();
            services.AddSingleton<ClientConfigurationFactory>();
            services.AddSingleton(prov =>
            {
                var config = prov.GetRequiredService<ServiceConfigurationFactory>().CreateConfig();
                config.Validate();
                return config;
            });

            services.AddMemoryCache();

            services.AddSingleton<IRadiusPacketParser, RadiusPacketParser>();
            services.AddSingleton<CacheService>();
            services.AddSingleton<MultiFactorApiClient>();
            services.AddSingleton<RadiusRouter>();
            services.AddSingleton<RadiusServer>();
            services.AddTransient<ChallengeProcessor>();

            services.AddSingleton<IFirstAuthFactorProcessor, LdapFirstAuthFactorProcessor>();
            services.AddSingleton<IFirstAuthFactorProcessor, RadiusFirstAuthFactorProcessor>();
            services.AddSingleton<IFirstAuthFactorProcessor, DefaultFirstAuthFactorProcessor>();
            services.AddSingleton<FirstAuthFactorProcessorProvider>();

            services.AddSingleton<UserGroupsGetterProvider>();
            services.AddSingleton<IUserGroupsGetter, ActiveDirectoryUserGroupsGetter>();
            services.AddSingleton<IUserGroupsGetter, DefaultUserGroupsGetter>();

            services.AddSingleton<BindIdentityFormatterFactory>();
            services.AddSingleton<LdapConnectionAdapterFactory>();
            services.AddSingleton<ProfileLoader>();
            services.AddSingleton<LdapService>();
            services.AddSingleton<MembershipVerifier>();

            services.AddSingleton(prov => new RandomWaiter(prov.GetRequiredService<IServiceConfiguration>().InvalidCredentialDelay));
            services.AddSingleton<AuthenticatedClientCache>();

            services.AddHostedService<ServerHost>();
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
