using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MultiFactor.Radius.Adapter.Core.Framework;
using MultiFactor.Radius.Adapter.Extensions;

namespace MultiFactor.Radius.Adapter.Tests.Fixtures;

internal static class TestHostFactory
{
    /// <summary>
    /// Creates a generic Radius Test Host with "Test" value of ASPNETCORE_ENVIRONMENT variable.
    /// </summary>
    /// <param name="configure">Configure host action.</param>
    /// <returns></returns>
    public static TestHost CreateHost(Action<RadiusHostApplicationBuilder>? configure = null)
    {
        var builder = CreateRadiusHostBuilder();

        builder.MockUdp();
        builder.WithTestConfiguration();
        builder.MockRadiusRequestPostProcessor();
        
        builder.Services.ReplaceService<ILoggerFactory, NullLoggerFactory>();

        builder.ConfigureApplication();

        configure?.Invoke(builder);
        var host = builder.Build();
        var testHost = new TestHost(host);
        return testHost;
    }

    /// <summary>
    /// Creates a generic Radius Host with "Test" value of ASPNETCORE_ENVIRONMENT variable.
    /// </summary>
    /// <returns></returns>
    public static RadiusHostApplicationBuilder CreateRadiusHostBuilder()
    {
        return RadiusHost.CreateApplicationBuilder(new[] { "--environment", "Test" });
    }
}
