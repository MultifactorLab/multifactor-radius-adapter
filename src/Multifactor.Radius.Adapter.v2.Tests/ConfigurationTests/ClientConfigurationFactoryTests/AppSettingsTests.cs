using Moq;
using Multifactor.Radius.Adapter.v2.Core.Auth;
using Multifactor.Radius.Adapter.v2.Core.Configuration.Client.Build;
using Multifactor.Radius.Adapter.v2.Core.Configuration.Service;
using Multifactor.Radius.Adapter.v2.Core.Radius.Attributes;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configuration.RadiusAdapter;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configuration.RadiusAdapter.Sections;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configuration.RadiusAdapter.Sections.LdapServer;

namespace Multifactor.Radius.Adapter.v2.Tests.ConfigurationTests.ClientConfigurationFactoryTests;

public class AppSettingsTests
{
    [Fact]
    public void CreateClientConfiguration_FirstFactorIsNone_ShouldReturnConfiguration()
    {
        var radiusConfig = new RadiusAdapterConfiguration()
        {
            AppSettings = new AppSettingsSection()
            {
                MultifactorNasIdentifier = "identifier",
                MultifactorSharedSecret = "secret",
                SignUpGroups = "groups",
                BypassSecondFactorWhenApiUnreachable = true,
                FirstFactorAuthenticationSource = "None",
                RadiusSharedSecret = "secret",
                PrivacyMode = "Full",
                PreAuthenticationMethod = "None",
                AuthenticationCacheLifetime = "00:01:00",
                AuthenticationCacheMinimalMatching = true,
                CallingStationIdAttribute = "12345",
                InvalidCredentialDelay = "3"
            },
            LdapServers = new LdapServersSection()
            {
                LdapServer = new[]
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

        var serviceConfig = new ServiceConfiguration();
        var configName = "name";
        var dictionaryMock = new Mock<IRadiusDictionary>();
        var attribute = new DictionaryAttribute("name", 1, "type");
        dictionaryMock.Setup(x => x.GetAttribute(It.IsAny<string>())).Returns(attribute);
        var factory =
            new ClientConfigurationFactory(dictionaryMock.Object);
        var clientConfig = factory.CreateConfig(configName, radiusConfig, serviceConfig);

        Assert.NotNull(clientConfig);
        Assert.Equal(AuthenticationSource.None, clientConfig.FirstFactorAuthenticationSource);
        Assert.Equal("secret", clientConfig.RadiusSharedSecret);
        Assert.NotNull(clientConfig.InvalidCredentialDelay);
        Assert.Equal(configName, clientConfig.Name);
        Assert.Equal("identifier", clientConfig.ApiCredential.Usr);
        Assert.Equal("secret", clientConfig.ApiCredential.Pwd);
        Assert.Equal("groups", clientConfig.SignUpGroups);
        Assert.NotNull(clientConfig.PrivacyMode);
        Assert.NotNull(clientConfig.PreAuthnMode);
        Assert.True(clientConfig.BypassSecondFactorWhenApiUnreachable);
        Assert.Equal("12345", clientConfig.CallingStationIdVendorAttribute);
        Assert.NotNull(clientConfig.AuthenticationCacheLifetime);
        Assert.Null(clientConfig.NpsServerEndpoint);
        Assert.Empty(clientConfig.RadiusReplyAttributes);
        Assert.NotNull(clientConfig.UserNameTransformRules);
        Assert.Null(clientConfig.ServiceClientEndpoint);
    }

    [Fact]
    public void CreateClientConfiguration_FirstFactorIsRadius_ShouldReturnConfiguration()
    {
        var radiusConfig = new RadiusAdapterConfiguration()
        {
            AppSettings = new AppSettingsSection()
            {
                MultifactorNasIdentifier = "identifier",
                MultifactorSharedSecret = "secret",
                SignUpGroups = "groups",
                BypassSecondFactorWhenApiUnreachable = true,
                FirstFactorAuthenticationSource = "Radius",
                AdapterClientEndpoint = "127.0.0.1",
                NpsServerEndpoint = "127.0.0.1",
                RadiusSharedSecret = "secret",
                PrivacyMode = "Full",
                PreAuthenticationMethod = "None",
                AuthenticationCacheLifetime = "00:01:00",
                AuthenticationCacheMinimalMatching = true,
                CallingStationIdAttribute = "12345",
                InvalidCredentialDelay = "3"
            },
            LdapServers = new LdapServersSection()
            {
                LdapServer = new[]
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

        var serviceConfig = new ServiceConfiguration();
        var configName = "name";
        var dictionaryMock = new Mock<IRadiusDictionary>();
        var attribute = new DictionaryAttribute("name", 1, "type");
        dictionaryMock.Setup(x => x.GetAttribute(It.IsAny<string>())).Returns(attribute);
        var factory =
            new ClientConfigurationFactory(dictionaryMock.Object);
        var clientConfig = factory.CreateConfig(configName, radiusConfig, serviceConfig);

        Assert.NotNull(clientConfig);
        Assert.Equal(AuthenticationSource.Radius, clientConfig.FirstFactorAuthenticationSource);
        Assert.Equal("secret", clientConfig.RadiusSharedSecret);
        Assert.NotNull(clientConfig.InvalidCredentialDelay);
        Assert.Equal(configName, clientConfig.Name);
        Assert.Equal("identifier", clientConfig.ApiCredential.Usr);
        Assert.Equal("secret", clientConfig.ApiCredential.Pwd);
        Assert.Equal("groups", clientConfig.SignUpGroups);
        Assert.NotNull(clientConfig.PrivacyMode);
        Assert.NotNull(clientConfig.PreAuthnMode);
        Assert.True(clientConfig.BypassSecondFactorWhenApiUnreachable);
        Assert.Equal("12345", clientConfig.CallingStationIdVendorAttribute);
        Assert.NotNull(clientConfig.AuthenticationCacheLifetime);
        Assert.NotNull(clientConfig.ServiceClientEndpoint);
        Assert.NotNull(clientConfig.NpsServerEndpoint);
        Assert.Empty(clientConfig.RadiusReplyAttributes);
        Assert.NotNull(clientConfig.UserNameTransformRules);
    }

    [Fact]
    public void CreateClientConfiguration_FirstFactorIsLdap_ShouldReturnConfiguration()
    {
        var radiusConfig = new RadiusAdapterConfiguration()
        {
            AppSettings = new AppSettingsSection()
            {
                MultifactorNasIdentifier = "identifier",
                MultifactorSharedSecret = "secret",
                SignUpGroups = "groups",
                BypassSecondFactorWhenApiUnreachable = true,
                FirstFactorAuthenticationSource = "Ldap",
                RadiusSharedSecret = "secret",
                PrivacyMode = "Full",
                PreAuthenticationMethod = "None",
                AuthenticationCacheLifetime = "00:01:00",
                AuthenticationCacheMinimalMatching = true,
                CallingStationIdAttribute = "12345",
                InvalidCredentialDelay = "3"
            },
            LdapServers = new LdapServersSection()
            {
                LdapServer = new[]
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

        var serviceConfig = new ServiceConfiguration();
        var configName = "name";
        var dictionaryMock = new Mock<IRadiusDictionary>();
        var attribute = new DictionaryAttribute("name", 1, "type");
        dictionaryMock.Setup(x => x.GetAttribute(It.IsAny<string>())).Returns(attribute);
        var factory =
            new ClientConfigurationFactory(dictionaryMock.Object);
        var clientConfig = factory.CreateConfig(configName, radiusConfig, serviceConfig);

        Assert.NotNull(clientConfig);
        Assert.Equal(AuthenticationSource.Ldap, clientConfig.FirstFactorAuthenticationSource);
        Assert.Equal("secret", clientConfig.RadiusSharedSecret);
        Assert.NotNull(clientConfig.InvalidCredentialDelay);
        Assert.Equal(configName, clientConfig.Name);
        Assert.Equal("identifier", clientConfig.ApiCredential.Usr);
        Assert.Equal("secret", clientConfig.ApiCredential.Pwd);
        Assert.Equal("groups", clientConfig.SignUpGroups);
        Assert.NotNull(clientConfig.PrivacyMode);
        Assert.NotNull(clientConfig.PreAuthnMode);
        Assert.True(clientConfig.BypassSecondFactorWhenApiUnreachable);
        Assert.Equal("12345", clientConfig.CallingStationIdVendorAttribute);
        Assert.NotNull(clientConfig.AuthenticationCacheLifetime);
        Assert.Empty(clientConfig.RadiusReplyAttributes);
        Assert.NotNull(clientConfig.UserNameTransformRules);
        Assert.Null(clientConfig.ServiceClientEndpoint);
    }
}