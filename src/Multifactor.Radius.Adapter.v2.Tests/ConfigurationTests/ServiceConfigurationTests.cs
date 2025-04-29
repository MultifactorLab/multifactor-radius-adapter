using System.Net;
using Moq;
using Multifactor.Radius.Adapter.v2.Core.Configuration.Client;
using Multifactor.Radius.Adapter.v2.Core.Configuration.Service;
using Multifactor.Radius.Adapter.v2.Core.RandomWaiterFeature;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configuration;

namespace Multifactor.Radius.Adapter.v2.Tests.ConfigurationTests;

public class ServiceConfigurationTests
{
    [Fact]
    public void BuildServiceConfiguration_ShouldBuild()
    {
        var configuration = new ServiceConfiguration();
        Assert.NotNull(configuration);
    }

    [Fact]
    public void SetApiProxy_ShouldSet()
    {
        var configuration = new ServiceConfiguration();
        configuration.SetApiProxy("proxy");

        Assert.Equal("proxy", configuration.ApiProxy);
    }

    [Fact]
    public void SetApiUrl_ShouldSet()
    {
        var configuration = new ServiceConfiguration();
        configuration.SetApiUrl("url");

        Assert.Equal("url", configuration.ApiUrl);
    }

    [Fact]
    public void SetApiTimeout_ShouldSet()
    {
        var configuration = new ServiceConfiguration();
        var timeout = TimeSpan.FromSeconds(5);
        configuration.SetApiTimeout(timeout);

        Assert.Equal(timeout, configuration.ApiTimeout);
    }

    [Fact]
    public void SetInvalidCredentialDelay_ShouldSet()
    {
        var configuration = new ServiceConfiguration();
        configuration.SetInvalidCredentialDelay(RandomWaiterConfig.Create("3"));

        Assert.NotNull(configuration.InvalidCredentialDelay);
    }

    [Fact]
    public void SetServiceServerEndpoint_ShouldSet()
    {
        var configuration = new ServiceConfiguration();
        IPEndPointFactory.TryParse("127.0.0.1", out var serviceServerEndpoint);
        configuration.SetServiceServerEndpoint(serviceServerEndpoint);

        Assert.NotNull(configuration.ServiceServerEndpoint);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SetIsSingleClientMode_ShouldSet(bool isSingleClientMode)
    {
        var configuration = new ServiceConfiguration();
        configuration.IsSingleClientMode(isSingleClientMode);

        Assert.Equal(isSingleClientMode, configuration.SingleClientMode);
    }

    [Fact]
    public void AddClientWithNasIdAsKey_ShouldAdd()
    {
        var configuration = new ServiceConfiguration();
        configuration.AddClient("key", new Mock<IClientConfiguration>().Object);

        Assert.Single(configuration.Clients);
        Assert.NotNull(configuration.GetClient("key"));
    }

    [Fact]
    public void AddClientWithIpAsKey_ShouldAdd()
    {
        var configuration = new ServiceConfiguration();
        var key = IPAddress.Parse("127.0.0.1");
        configuration.AddClient(key, new Mock<IClientConfiguration>().Object);

        Assert.Single(configuration.Clients);
        Assert.NotNull(configuration.GetClient(key));
    }
}