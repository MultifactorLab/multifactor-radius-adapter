using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MultiFactor.Radius.Adapter.HostedServices;
using MultiFactor.Radius.Adapter.Server;
using MultiFactor.Radius.Adapter.Core.Radius;
using MultiFactor.Radius.Adapter.Services;
using MultiFactor.Radius.Adapter.Configuration.ConfigurationLoading;
using MultiFactor.Radius.Adapter.Configuration.Core;
using MultiFactor.Radius.Adapter.Configuration;
using MultiFactor.Radius.Adapter.Core.Radius.Attributes;
using Microsoft.AspNetCore.Http;
using System.Linq.Expressions;
using Microsoft.Extensions.Logging;
using Serilog;
using System;
using System.Net;
using MultiFactor.Radius.Adapter.Core;
using MultiFactor.Radius.Adapter.Framework.Pipeline;
using MultiFactor.Radius.Adapter.Server.Pipeline.PostProcessing;

namespace MultiFactor.Radius.Adapter.Framework;

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

        builder.Services.AddSingleton(prov => ApplicationVariablesFactory.Create());
        builder.Services.AddConfiguration();

        builder.Services.AddHostedService<ServerHost>();
        builder.Services.AddHostedService<Starter>();

        builder.Services.AddSingleton<RadiusServer>();
        builder.Services.AddSingleton<RadiusContextFactory>();

        builder.Services.AddSingleton<IRadiusRequestPostProcessor, RadiusRequestPostProcessor>();
        builder.Services.AddTransient<Func<IPEndPoint, IUdpClient>>(prov => endpoint => new RealUdpClient(endpoint));
        builder.Services.AddSingleton<RadiusResponseSenderFactory>();
        builder.Services.AddSingleton<RadiusReplyAttributeEnricher>();

        builder.Services.AddMemoryCache();

        builder.Services.AddSingleton<IRadiusDictionary, RadiusDictionary>();
        builder.Services.AddSingleton<IRadiusPacketParser, RadiusPacketParser>();
        builder.Services.AddSingleton<CacheService>();

        builder.Services.AddSingleton(prov => new RandomWaiter(prov.GetRequiredService<IServiceConfiguration>().InvalidCredentialDelay));

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
