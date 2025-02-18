using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MultiFactor.Radius.Adapter.Core.Radius.Attributes;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration.ConfigurationLoading;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration.Exceptions;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration.Models;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration.RootLevel;

namespace MultiFactor.Radius.Adapter.Tests.AdapterConfig;

public class ClientConfigurationFactoryTests
{
    [Fact]
    public void Create_NoDomainHasADGroups_ShouldThrow()
    {
        var configName = "name";
        var appSettings = new AppSettingsSection()
        {
            ActiveDirectoryGroup = "group",
            FirstFactorAuthenticationSource = "None",
            RadiusSharedSecret = "000",
            MultifactorNasIdentifier = "123",
            MultifactorSharedSecret = "123",
            InvalidCredentialDelay = "1"
        };
        var config = new RadiusAdapterConfiguration() { AppSettings = appSettings };
        var dictMock = new Mock<IRadiusDictionary>();
        var factory = new ClientConfigurationFactory(dictMock.Object, NullLogger<ClientConfigurationFactory>.Instance);
        var serviceConfiguration = new ServiceConfiguration();
        
        var ex = Assert.Throws<InvalidConfigurationException>(() => factory.CreateConfig(configName, config, serviceConfiguration));
        
        var expected = InvalidConfigurationException.For(
            x => x.AppSettings.ActiveDirectoryDomain,
            "Membership verification impossible: '{prop}' element not found. Config name: '{0}'", configName);
        
        Assert.Equal(expected.Message, ex.Message);
    }
}