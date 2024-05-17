using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MultiFactor.Radius.Adapter.Configuration.Core;
using MultiFactor.Radius.Adapter.Extensions;
using MultiFactor.Radius.Adapter.Framework;
using MultiFactor.Radius.Adapter.Framework.Pipeline;
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
        builder.Services.ReplaceService<IRootConfigurationProvider, TestRootConfigProvider>();
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
