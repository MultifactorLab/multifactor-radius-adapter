using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using MultiFactor.Radius.Adapter.Core.Framework;
using MultiFactor.Radius.Adapter.Core.Framework.Pipeline;
using MultiFactor.Radius.Adapter.Extensions;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration.ConfigurationLoading;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration.Core;
using MultiFactor.Radius.Adapter.Tests.Fixtures.ConfigLoading;
using System.Net;

namespace MultiFactor.Radius.Adapter.Tests.Fixtures;

internal static class TestHostFactory
{
    /// <summary>
    /// Creates a generic Radius Test Host.
    /// </summary>
    /// <param name="configure">Configure host action.</param>
    /// <returns></returns>
    public static TestHost CreateHost(Action<RadiusHostApplicationBuilder>? configure = null)
    {
        var builder = RadiusHost.CreateApplicationBuilder(new[] { "--environment", "Test" });

        builder.Services.ReplaceService<Func<IPEndPoint, IUdpClient>>(prov => endpoint => new Mock<IUdpClient>().Object);

        builder.Services.AddOptions<TestConfigProviderOptions>();
        builder.Services.ReplaceService(prov =>
        {
            var opt = prov.GetRequiredService<IOptions<TestConfigProviderOptions>>().Value;
            var rootConfig = TestRootConfigProvider.GetRootConfiguration(opt);
            var factory = prov.GetRequiredService<ServiceConfigurationFactory>();

            var config = factory.CreateConfig(rootConfig);
            config.Validate();

            return config;
        });

        builder.Services.ReplaceService<IClientConfigurationsProvider, TestClientConfigsProvider>();

        builder.Services.ReplaceService(new Mock<IRadiusRequestPostProcessor>().Object);

        builder.Services.ReplaceService<ILoggerFactory, NullLoggerFactory>();

        builder.ConfigureApplication();

        configure?.Invoke(builder);
        var host = builder.Build();
        var testHost = new TestHost(host);
        return testHost;
    }
}
