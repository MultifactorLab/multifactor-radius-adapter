using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MultiFactor.Radius.Adapter.HostedServices;
using MultiFactor.Radius.Adapter.Server.Context;
using MultiFactor.Radius.Adapter.Server;
using MultiFactor.Radius.Adapter.Core.Radius;
using MultiFactor.Radius.Adapter.Services;
using MultiFactor.Radius.Adapter.Configuration.ConfigurationLoading;
using MultiFactor.Radius.Adapter.Configuration.Core;
using MultiFactor.Radius.Adapter.Configuration;
using MultiFactor.Radius.Adapter.Core.Radius.Attributes;
using Microsoft.AspNetCore.Http;
using System.Linq.Expressions;
using MultiFactor.Radius.Adapter.Server.Pipeline;

namespace MultiFactor.Radius.Adapter.Core;

internal static class RadiusHost
{
    public static RadiusHostApplicationBuilder CreateApplicationBuilder(string[] args = null)
    {
        var builder = Host.CreateApplicationBuilder(args);

        builder.Services.AddSingleton(prov => ApplicationVariablesFactory.Create());
        builder.Services.AddSingleton<IRootConfigurationProvider, DefaultRootConfigurationProvider>();

        builder.Services.AddConfiguration();

        builder.Services.AddHostedService<ServerHost>();
        builder.Services.AddHostedService<Starter>();

        builder.Services.AddSingleton<RadiusServer>();
        builder.Services.AddSingleton<RadiusContextFactory>();

        builder.Services.AddSingleton<IRadiusRequestPostProcessor, RadiusRequestPostProcessor>();
        builder.Services.AddSingleton<RadiusResponseSenderFactory>();
        builder.Services.AddSingleton<RadiusReplyAttributeEnricher>();

        builder.Services.AddMemoryCache();

        builder.Services.AddSingleton<IRadiusPacketParser, RadiusPacketParser>();
        builder.Services.AddSingleton<CacheService>();

        builder.Services.AddSingleton(prov => new RandomWaiter(prov.GetRequiredService<IServiceConfiguration>().InvalidCredentialDelay));

        return new RadiusHostApplicationBuilder(builder);
    }

    private static void AddConfiguration(this IServiceCollection services)
    {
        services.AddSingleton<IClientConfigurationsProvider, DefaultClientConfigurationsProvider>();

        services.AddSingleton<IRadiusDictionary, RadiusDictionary>();

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
