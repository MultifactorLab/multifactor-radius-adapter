using Microsoft.Extensions.DependencyInjection;
using Moq;
using MultiFactor.Radius.Adapter.Configuration.Core;
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

        builder.ConfigureApplication();

        configure?.Invoke(builder);
        return new TestHost(builder.Build());
    }

    /// <summary>
    /// Creates Radius Test Host and prepare it for pipeline testing
    /// </summary>
    /// <param name="configure">Configure host action.</param>
    /// <returns></returns>
    public static TestHost CreatePipelineTestHost(Action<RadiusHostApplicationBuilder>? configure = null)
    {
        return CreateHost(builder =>
        {
            builder.Services.RemoveService<IRadiusPipeline>();
            builder.Services.AddSingleton<RadiusPipeline>();
            builder.Services.AddSingleton<IRadiusPipeline>(prov => prov.GetRequiredService<RadiusPipeline>());

            configure?.Invoke(builder);
        });
    }
}
