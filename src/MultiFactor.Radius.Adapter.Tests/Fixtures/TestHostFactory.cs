using Moq;
using MultiFactor.Radius.Adapter.Configuration.Core;
using MultiFactor.Radius.Adapter.Core;
using MultiFactor.Radius.Adapter.Tests.Fixtures.ConfigLoading;
using System.Net;

namespace MultiFactor.Radius.Adapter.Tests.Fixtures;

internal static class TestHostFactory
{
    public static TestHost CreateHost(Action<RadiusHostApplicationBuilder>? configure = null)
    {
        var builder = RadiusHost.CreateApplicationBuilder(new[] { "--environment", "Test" });

        builder.Services.ReplaceService<Func<IPEndPoint, IUdpClient>>(prov => endpoint => new Mock<IUdpClient>().Object);
        builder.Services.ReplaceService<IRootConfigurationProvider, TestRootConfigProvider>();
        builder.Services.ReplaceService<IClientConfigurationsProvider, TestClientConfigsProvider>();

        builder.ConfigureApplication();

        configure?.Invoke(builder);
        return new TestHost(builder.Build());
    }
}
