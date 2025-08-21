using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Multifactor.Radius.Adapter.v2.Core;
using Multifactor.Radius.Adapter.v2.Core.Configuration.Client;
using Multifactor.Radius.Adapter.v2.Core.Configuration.Client.Build;
using Multifactor.Radius.Adapter.v2.Core.Configuration.Service;
using Multifactor.Radius.Adapter.v2.Core.Configuration.Service.Build;
using Multifactor.Radius.Adapter.v2.Core.Radius.Attributes;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configuration.RadiusAdapter;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configuration.RadiusAdapter.Sections;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configuration.RadiusAdapter.Sections.LdapServer;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configuration.XmlAppConfiguration;
using LdapServerConfiguration =
    Multifactor.Radius.Adapter.v2.Infrastructure.Configuration.RadiusAdapter.Sections.LdapServer.LdapServerConfiguration;

namespace Multifactor.Radius.Adapter.v2.Tests.ConfigurationTests;

public class ServiceConfigurationFactoryTests
{
    [Fact]
    public void CreateServiceConfiguration_SingleConfig_ShouldCreate()
    {
        var clientConfigurationProviderMock = new Mock<IClientConfigurationsProvider>();
        clientConfigurationProviderMock.Setup(x => x.GetClientConfigurations()).Returns([]);
        var dictionaryMock = new Mock<IRadiusDictionary>();
        var attribute = new DictionaryAttribute("name", 1, "type");
        dictionaryMock.Setup(x => x.GetAttribute(It.IsAny<string>())).Returns(attribute);

        var clientFactoryMock = new Mock<IClientConfigurationFactory>();
        clientFactoryMock
            .Setup(
                x => x.CreateConfig(
                    It.IsAny<string>(),
                    It.IsAny<RadiusAdapterConfiguration>(),
                    It.IsAny<IServiceConfiguration>()))
            .Returns(new Mock<IClientConfiguration>().Object);

        var serviceFactory = new ServiceConfigurationFactory(
            clientConfigurationProviderMock.Object,
            clientFactoryMock.Object,
            NullLogger<ServiceConfigurationFactory>.Instance);
        var serviceConfiguration = serviceFactory.CreateConfig(GetConfiguration());

        Assert.NotNull(serviceConfiguration);
        Assert.Equal("url", serviceConfiguration.ApiUrls[0]);
        Assert.Equal("proxy", serviceConfiguration.ApiProxy);
        Assert.Equal(TimeSpan.FromMinutes(2), serviceConfiguration.ApiTimeout);
        Assert.True(serviceConfiguration.SingleClientMode);
        Assert.NotNull(serviceConfiguration.InvalidCredentialDelay);
        Assert.NotNull(serviceConfiguration.ServiceServerEndpoint);
        Assert.Single(serviceConfiguration.Clients);
    }

    [Fact]
    public void CreateServiceConfiguration_NasIdentifierAsClientId_ShouldCreate()
    {
        var clientConfigurationProviderMock = new Mock<IClientConfigurationsProvider>();
        var clientAdapterConfig1 = new RadiusAdapterConfiguration()
        {
            AppSettings = new AppSettingsSection()
            {
                MultifactorApiUrl = "http://127.0.0.1",
                RadiusClientNasIdentifier = "clientNasIdentifier1",
            }
        };

        var clientAdapterConfig2 = new RadiusAdapterConfiguration()
        {
            AppSettings = new AppSettingsSection()
            {
                MultifactorApiUrl = "http://127.0.0.1",
                RadiusClientNasIdentifier = "clientNasIdentifier2",
            }
        };

        clientConfigurationProviderMock
            .Setup(x => x.GetClientConfigurations())
            .Returns(new[] { clientAdapterConfig1, clientAdapterConfig2 });

        clientConfigurationProviderMock
            .Setup(x => x.GetSource(It.IsAny<RadiusAdapterConfiguration>()))
            .Returns(new FileMock());

        var dictionaryMock = new Mock<IRadiusDictionary>();
        var attribute = new DictionaryAttribute("name", 1, "type");
        dictionaryMock.Setup(x => x.GetAttribute(It.IsAny<string>())).Returns(attribute);

        var clientFactoryMock = new Mock<IClientConfigurationFactory>();

        clientFactoryMock.Setup(x => x.CreateConfig(It.IsAny<string>(), It.IsAny<RadiusAdapterConfiguration>(),
                It.IsAny<IServiceConfiguration>()))
            .Returns(new Mock<IClientConfiguration>().Object);

        var serviceFactory = new ServiceConfigurationFactory(
            clientConfigurationProviderMock.Object,
            clientFactoryMock.Object,
            NullLogger<ServiceConfigurationFactory>.Instance);

        var serviceConfiguration = serviceFactory.CreateConfig(GetConfiguration());

        Assert.NotNull(serviceConfiguration);
        Assert.Equal("url", serviceConfiguration.ApiUrls[0]);
        Assert.Equal("proxy", serviceConfiguration.ApiProxy);
        Assert.Equal(TimeSpan.FromMinutes(2), serviceConfiguration.ApiTimeout);
        Assert.False(serviceConfiguration.SingleClientMode);
        Assert.NotNull(serviceConfiguration.InvalidCredentialDelay);
        Assert.NotNull(serviceConfiguration.ServiceServerEndpoint);
        Assert.Equal(2, serviceConfiguration.Clients.Count);
    }

    [Fact]
    public void CreateServiceConfiguration_IpAsClientId_ShouldCreate()
    {
        var clientConfigurationProviderMock = new Mock<IClientConfigurationsProvider>();
        var clientAdapterConfig1 = new RadiusAdapterConfiguration()
        {
            AppSettings = new AppSettingsSection()
            {
                MultifactorApiUrl = "http://127.0.0.1",
                RadiusClientIp = "127.0.0.1",
            }
        };

        var clientAdapterConfig2 = new RadiusAdapterConfiguration()
        {
            AppSettings = new AppSettingsSection()
            {
                MultifactorApiUrl = "http://127.0.0.1",
                RadiusClientNasIdentifier = "127.0.0.2",
            }
        };

        clientConfigurationProviderMock
            .Setup(x => x.GetClientConfigurations())
            .Returns(new[] { clientAdapterConfig1, clientAdapterConfig2 });

        clientConfigurationProviderMock
            .Setup(x => x.GetSource(It.IsAny<RadiusAdapterConfiguration>()))
            .Returns(new FileMock());

        var dictionaryMock = new Mock<IRadiusDictionary>();
        var attribute = new DictionaryAttribute("name", 1, "type");
        dictionaryMock.Setup(x => x.GetAttribute(It.IsAny<string>())).Returns(attribute);

        var clientFactoryMock = new Mock<IClientConfigurationFactory>();

        clientFactoryMock
            .Setup(
                x => x.CreateConfig(
                    It.IsAny<string>(),
                    It.IsAny<RadiusAdapterConfiguration>(),
                    It.IsAny<IServiceConfiguration>()))
            .Returns(new Mock<IClientConfiguration>().Object);

        var serviceFactory = new ServiceConfigurationFactory(
            clientConfigurationProviderMock.Object,
            clientFactoryMock.Object,
            NullLogger<ServiceConfigurationFactory>.Instance);

        var serviceConfiguration = serviceFactory.CreateConfig(GetConfiguration());

        Assert.NotNull(serviceConfiguration);
        Assert.Equal("url", serviceConfiguration.ApiUrls[0]);
        Assert.Equal("proxy", serviceConfiguration.ApiProxy);
        Assert.Equal(TimeSpan.FromMinutes(2), serviceConfiguration.ApiTimeout);
        Assert.False(serviceConfiguration.SingleClientMode);
        Assert.NotNull(serviceConfiguration.InvalidCredentialDelay);
        Assert.NotNull(serviceConfiguration.ServiceServerEndpoint);
        Assert.Equal(2, serviceConfiguration.Clients.Count);
    }
    
    [Theory]
    [InlineData("url")]
    [InlineData("url1;url2")]
    [InlineData("url;url2;url3")]
    public void CreateServiceConfiguration_MultipleMfApiUrls_ShouldCreate(string urls)
    {
        var clientConfigurationProviderMock = new Mock<IClientConfigurationsProvider>();
        clientConfigurationProviderMock.Setup(x => x.GetClientConfigurations()).Returns([]);
        var dictionaryMock = new Mock<IRadiusDictionary>();
        var attribute = new DictionaryAttribute("name", 1, "type");
        dictionaryMock.Setup(x => x.GetAttribute(It.IsAny<string>())).Returns(attribute);

        var clientFactoryMock = new Mock<IClientConfigurationFactory>();
        clientFactoryMock
            .Setup(
                x => x.CreateConfig(
                    It.IsAny<string>(),
                    It.IsAny<RadiusAdapterConfiguration>(),
                    It.IsAny<IServiceConfiguration>()))
            .Returns(new Mock<IClientConfiguration>().Object);

        var serviceFactory = new ServiceConfigurationFactory(
            clientConfigurationProviderMock.Object,
            clientFactoryMock.Object,
            NullLogger<ServiceConfigurationFactory>.Instance);
        var config = GetConfiguration(urls);
        var serviceConfiguration = serviceFactory.CreateConfig(config);

        var expectedUrls = Utils.SplitString(urls);
        var actualUrls = serviceConfiguration.ApiUrls;
        Assert.True(expectedUrls.SequenceEqual(actualUrls));
    }

    private RadiusAdapterConfiguration GetConfiguration(string apiUrls = "url") => new RadiusAdapterConfiguration()
    {
        AppSettings = new AppSettingsSection()
        {
            MultifactorApiUrl = apiUrls,
            MultifactorApiProxy = "proxy",
            MultifactorApiTimeout = "00:02:00",
            AdapterServerEndpoint = "127.0.0.1",
            InvalidCredentialDelay = "3",
        },
        LdapServers = new LdapServersSection()
        {
            LdapServers = new[]
            {
                new LdapServerConfiguration()
                {
                    ConnectionString = "connectionString",
                    UserName = "username",
                    Password = "password",
                }
            }
        }
    };

    private class FileMock : RadiusConfigurationSource
    {
        public override string Name => "File";
    }
}