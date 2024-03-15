﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MultiFactor.Radius.Adapter.Configuration.Core;
using MultiFactor.Radius.Adapter.Core;
using MultiFactor.Radius.Adapter.Tests.Fixtures.ConfigLoading;

namespace MultiFactor.Radius.Adapter.Tests.Fixtures;

internal static class TestHostFactory
{
    public static IHost CreateHost(Action<IServiceCollection>? configureServices = null)
    {
        var builder = RadiusHost.CreateApplicationBuilder();
        builder.Configure(x =>
        {
            x.ConfigureApplication(services =>
            {
                services.ReplaceService<IRootConfigurationProvider, TestRootConfigProvider>();
                services.ReplaceService<IClientConfigurationsProvider, TestClientConfigsProvider>();
                configureServices?.Invoke(services);
            });
        });
        return builder.Build();
    }
}