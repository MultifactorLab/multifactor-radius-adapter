using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MultiFactor.Radius.Adapter.Core.Framework.Pipeline;
using MultiFactor.Radius.Adapter.Core.Radius;
using MultiFactor.Radius.Adapter.Core.Radius.Attributes;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration.ConfigurationLoading;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration.Core;
using MultiFactor.Radius.Adapter.Server;
using MultiFactor.Radius.Adapter.Server.Pipeline.PostProcessing;
using MultiFactor.Radius.Adapter.Services;
using System;
using System.IO;
using System.Reflection;

namespace MultiFactor.Radius.Adapter.Core.Framework;

internal static class RadiusHost
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RadiusHostApplicationBuilder"/> class with pre-configured defaults.
    /// </summary>
    /// <param name="args">The command line args.</param>
    /// <returns>The initialized <see cref="RadiusHostApplicationBuilder"/>.</returns>
    public static RadiusHostApplicationBuilder CreateApplicationBuilder(string[] args = null)
    {
        var builder = Host.CreateApplicationBuilder(args);

        builder.Services.AddSingleton(prov =>
        {
            return new ApplicationVariables
            {
                AppPath = Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory),
                AppVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString(),
                StartedAt = DateTime.Now
            };
        });
        builder.Services.AddConfiguration();

        builder.Services.AddHostedService<ServerHost>();

        builder.Services.AddSingleton<RadiusServer>();
        builder.Services.AddSingleton<IUdpClient, RealUdpClient>();
        builder.Services.AddSingleton<RadiusContextFactory>();

        builder.Services.AddSingleton<IRadiusRequestPostProcessor, RadiusRequestPostProcessor>();
        builder.Services.AddSingleton<IRadiusResponseSender, RadiusResponseSender>();
        builder.Services.AddSingleton<RadiusReplyAttributeEnricher>();

        builder.Services.AddMemoryCache();

        builder.Services.AddSingleton<RadiusDictionary>();
        builder.Services.AddSingleton<IRadiusDictionary>(prov =>
        {
            var dict = prov.GetRequiredService<RadiusDictionary>();
            dict.Read();
            return dict;
        });
        builder.Services.AddSingleton<RadiusPacketParser>();
        builder.Services.AddSingleton<CacheService>();

        return new RadiusHostApplicationBuilder(builder);
    }

    private static void AddConfiguration(this IServiceCollection services)
    {
        services.AddSingleton<IRootConfigurationProvider, DefaultRootConfigurationProvider>();
        services.AddSingleton<IClientConfigurationsProvider, DefaultClientConfigurationsProvider>();

        services.AddSingleton<ServiceConfigurationFactory>();
        services.AddSingleton<ClientConfigurationFactory>();

        services.AddSingleton(prov =>
        {
            var config = prov.GetRequiredService<ServiceConfigurationFactory>().CreateConfig();
            config.Validate();
            return config;
        });
    }
}
