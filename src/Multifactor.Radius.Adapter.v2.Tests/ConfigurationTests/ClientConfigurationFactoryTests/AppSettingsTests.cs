using System.Net;
using Moq;
using Multifactor.Radius.Adapter.v2.Domain;
using Multifactor.Radius.Adapter.v2.Domain.Auth;
using Multifactor.Radius.Adapter.v2.Domain.Radius.Attributes;
using Multifactor.Radius.Adapter.v2.Domain.RadiusAdapter;
using Multifactor.Radius.Adapter.v2.Domain.RadiusAdapter.Sections;
using Multifactor.Radius.Adapter.v2.Domain.RadiusAdapter.Sections.LdapServer;
using Multifactor.Radius.Adapter.v2.Domain.RadiusAdapter.Sections.RadiusReply;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configuration.Client;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configuration.Service;
using Multifactor.Radius.Adapter.v2.Infrastructure.Exceptions;
using Multifactor.Radius.Adapter.v2.Tests.Fixture;
using NetTools;
using LdapServerConfiguration = Multifactor.Radius.Adapter.v2.Domain.RadiusAdapter.Sections.LdapServer.LdapServerConfiguration;

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
                MultifactorApiUrl = "http://127.0.0.1",
                MultifactorNasIdentifier = "identifier",
                MultifactorSharedSecret = "secret",
                SignUpGroups = "groups",
                BypassSecondFactorWhenApiUnreachable = true,
                FirstFactorAuthenticationSource = "None",
                RadiusSharedSecret = "secret",
                PrivacyMode = "Full",
                PreAuthenticationMethod = "None",
                AuthenticationCacheLifetime = "00:01:00",
                CallingStationIdAttribute = "12345",
                InvalidCredentialDelay = "3"
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
        Assert.Empty(clientConfig.NpsServerEndpoints);
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
                MultifactorApiUrl = "http://127.0.0.1",
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
                CallingStationIdAttribute = "12345",
                InvalidCredentialDelay = "3"
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
        Assert.NotNull(clientConfig.NpsServerEndpoints);
        Assert.Single(clientConfig.NpsServerEndpoints);
        var nps = clientConfig.NpsServerEndpoints.First();
        Assert.Equal(IPEndPoint.Parse("127.0.0.1"), nps);
        Assert.Empty(clientConfig.RadiusReplyAttributes);
        Assert.NotNull(clientConfig.UserNameTransformRules);
    }
    
    [Theory]
    [InlineData("invalid-nps-server")]
    [InlineData("127.0.0.1; invalid-nps-server")]
    [InlineData("127.0.0.1; invalid-nps-server; 127.0.0.2")]
    public void CreateClientConfiguration_InvalidNpsServer_ShouldThrow(string npsSetting)
    {
        var radiusConfig = new RadiusAdapterConfiguration()
        {
            AppSettings = new AppSettingsSection()
            {
                MultifactorApiUrl = "http://127.0.0.1",
                MultifactorNasIdentifier = "identifier",
                MultifactorSharedSecret = "secret",
                SignUpGroups = "groups",
                BypassSecondFactorWhenApiUnreachable = true,
                FirstFactorAuthenticationSource = "Radius",
                AdapterClientEndpoint = "127.0.0.1",
                NpsServerEndpoint = npsSetting,
                RadiusSharedSecret = "secret",
                PrivacyMode = "Full",
                PreAuthenticationMethod = "None",
                AuthenticationCacheLifetime = "00:01:00",
                CallingStationIdAttribute = "12345",
                InvalidCredentialDelay = "3"
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

        var serviceConfig = new ServiceConfiguration();
        var configName = "name";
        var dictionaryMock = new Mock<IRadiusDictionary>();
        var attribute = new DictionaryAttribute("name", 1, "type");
        dictionaryMock.Setup(x => x.GetAttribute(It.IsAny<string>())).Returns(attribute);
        var factory =
            new ClientConfigurationFactory(dictionaryMock.Object);
       var ex = Assert.Throws<InvalidConfigurationException>(() => factory.CreateConfig(configName, radiusConfig, serviceConfig));
       Assert.Contains("Invalid NPS", ex.Message);
    }
    
    [Theory]
    [InlineData("127.0.0.1:123")]
    [InlineData("127.0.0.1; 127.0.0.2:123")]
    [InlineData("127.0.0.1; 127.0.0.2; 127.0.0.3:123")]
    public void CreateClientConfiguration_MultipleNpsServers_ShouldReturnConfiguration(string npsServers)
    {
        var expectedNpsServers = Utils.SplitString(npsServers).Select(IPEndPoint.Parse);
        var radiusConfig = new RadiusAdapterConfiguration()
        {
            AppSettings = new AppSettingsSection()
            {
                MultifactorApiUrl = "http://127.0.0.1",
                MultifactorNasIdentifier = "identifier",
                MultifactorSharedSecret = "secret",
                SignUpGroups = "groups",
                BypassSecondFactorWhenApiUnreachable = true,
                FirstFactorAuthenticationSource = "Radius",
                AdapterClientEndpoint = "127.0.0.1",
                NpsServerEndpoint = npsServers,
                RadiusSharedSecret = "secret",
                PrivacyMode = "Full",
                PreAuthenticationMethod = "None",
                AuthenticationCacheLifetime = "00:01:00",
                CallingStationIdAttribute = "12345",
                InvalidCredentialDelay = "3"
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

        var serviceConfig = new ServiceConfiguration();
        var configName = "name";
        var dictionaryMock = new Mock<IRadiusDictionary>();
        var attribute = new DictionaryAttribute("name", 1, "type");
        dictionaryMock.Setup(x => x.GetAttribute(It.IsAny<string>())).Returns(attribute);
        var factory =
            new ClientConfigurationFactory(dictionaryMock.Object);
        var clientConfig = factory.CreateConfig(configName, radiusConfig, serviceConfig);

        Assert.NotNull(clientConfig);
        Assert.True(expectedNpsServers.SequenceEqual(clientConfig.NpsServerEndpoints));
    }
    
    [Fact]
    public void CreateClientConfiguration_NpsServerTimeout_ShouldReturnTimeout()
    {
        var expectedNpsTimeout = TimeSpan.FromSeconds(30);
        var radiusConfig = new RadiusAdapterConfiguration()
        {
            AppSettings = new AppSettingsSection()
            {
                MultifactorApiUrl = "http://127.0.0.1",
                MultifactorNasIdentifier = "identifier",
                MultifactorSharedSecret = "secret",
                SignUpGroups = "groups",
                BypassSecondFactorWhenApiUnreachable = true,
                FirstFactorAuthenticationSource = "Radius",
                AdapterClientEndpoint = "127.0.0.1",
                NpsServerEndpoint = "127.0.0.1",
                NpsServerTimeout = "00:00:30",
                RadiusSharedSecret = "secret",
                PrivacyMode = "Full",
                PreAuthenticationMethod = "None",
                AuthenticationCacheLifetime = "00:01:00",
                CallingStationIdAttribute = "12345",
                InvalidCredentialDelay = "3"
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

        var serviceConfig = new ServiceConfiguration();
        var configName = "name";
        var dictionaryMock = new Mock<IRadiusDictionary>();
        var attribute = new DictionaryAttribute("name", 1, "type");
        dictionaryMock.Setup(x => x.GetAttribute(It.IsAny<string>())).Returns(attribute);
        var factory =
            new ClientConfigurationFactory(dictionaryMock.Object);
        var clientConfig = factory.CreateConfig(configName, radiusConfig, serviceConfig);

        Assert.NotNull(clientConfig);
        Assert.Equal(expectedNpsTimeout, clientConfig.NpsServerTimeout);
    }
    
    [Fact]
    public void CreateClientConfiguration_NoNpsServerTimeout_ShouldReturnDefaultTimeout()
    {
        var expectedNpsTimeout = TimeSpan.FromSeconds(5);
        var radiusConfig = new RadiusAdapterConfiguration()
        {
            AppSettings = new AppSettingsSection()
            {
                MultifactorApiUrl = "http://127.0.0.1",
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
                CallingStationIdAttribute = "12345",
                InvalidCredentialDelay = "3"
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

        var serviceConfig = new ServiceConfiguration();
        var configName = "name";
        var dictionaryMock = new Mock<IRadiusDictionary>();
        var attribute = new DictionaryAttribute("name", 1, "type");
        dictionaryMock.Setup(x => x.GetAttribute(It.IsAny<string>())).Returns(attribute);
        var factory =
            new ClientConfigurationFactory(dictionaryMock.Object);
        var clientConfig = factory.CreateConfig(configName, radiusConfig, serviceConfig);

        Assert.NotNull(clientConfig);
        Assert.Equal(expectedNpsTimeout, clientConfig.NpsServerTimeout);
    }
    
    [Fact]
    public void CreateClientConfiguration_InvalidNpsServerTimeout_ShouldThrow()
    {
        var radiusConfig = new RadiusAdapterConfiguration()
        {
            AppSettings = new AppSettingsSection()
            {
                MultifactorApiUrl = "http://127.0.0.1",
                MultifactorNasIdentifier = "identifier",
                MultifactorSharedSecret = "secret",
                SignUpGroups = "groups",
                BypassSecondFactorWhenApiUnreachable = true,
                FirstFactorAuthenticationSource = "Radius",
                AdapterClientEndpoint = "127.0.0.1",
                NpsServerEndpoint = "127.0.0.1",
                NpsServerTimeout = "random",
                RadiusSharedSecret = "secret",
                PrivacyMode = "Full",
                PreAuthenticationMethod = "None",
                AuthenticationCacheLifetime = "00:01:00",
                CallingStationIdAttribute = "12345",
                InvalidCredentialDelay = "3"
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

        var serviceConfig = new ServiceConfiguration();
        var configName = "name";
        var dictionaryMock = new Mock<IRadiusDictionary>();
        var attribute = new DictionaryAttribute("name", 1, "type");
        dictionaryMock.Setup(x => x.GetAttribute(It.IsAny<string>())).Returns(attribute);
        var factory =
            new ClientConfigurationFactory(dictionaryMock.Object);
        var ex = Assert.Throws<InvalidConfigurationException>(() => factory.CreateConfig(configName, radiusConfig, serviceConfig));
        Assert.Contains("Invalid NPS server timeout", ex.Message);
    }

    [Fact]
    public void CreateClientConfiguration_FirstFactorIsLdap_ShouldReturnConfiguration()
    {
        var radiusConfig = new RadiusAdapterConfiguration()
        {
            AppSettings = new AppSettingsSection()
            {
                MultifactorApiUrl = "http://127.0.0.1",
                MultifactorNasIdentifier = "identifier",
                MultifactorSharedSecret = "secret",
                SignUpGroups = "groups",
                BypassSecondFactorWhenApiUnreachable = true,
                FirstFactorAuthenticationSource = "Ldap",
                RadiusSharedSecret = "secret",
                PrivacyMode = "Full",
                PreAuthenticationMethod = "None",
                AuthenticationCacheLifetime = "00:01:00",
                CallingStationIdAttribute = "12345",
                InvalidCredentialDelay = "3"
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
    
    [Fact]
    public void CreateClientConfiguration_FirstFactorIsLdapNoServers_ShouldThrow()
    {
        var radiusConfig = new RadiusAdapterConfiguration()
        {
            AppSettings = new AppSettingsSection()
            {
                MultifactorApiUrl = "http://127.0.0.1",
                MultifactorNasIdentifier = "identifier",
                MultifactorSharedSecret = "secret",
                SignUpGroups = "groups",
                BypassSecondFactorWhenApiUnreachable = true,
                FirstFactorAuthenticationSource = "Ldap",
                RadiusSharedSecret = "secret",
                PrivacyMode = "Full",
                PreAuthenticationMethod = "None",
                AuthenticationCacheLifetime = "00:01:00",
                CallingStationIdAttribute = "12345",
                InvalidCredentialDelay = "3"
            }
        };

        var serviceConfig = new ServiceConfiguration();
        var configName = "name";
        var dictionaryMock = new Mock<IRadiusDictionary>();
        var attribute = new DictionaryAttribute("name", 1, "type");
        dictionaryMock.Setup(x => x.GetAttribute(It.IsAny<string>())).Returns(attribute);
        var factory = new ClientConfigurationFactory(dictionaryMock.Object);
        Assert.Throws<InvalidConfigurationException>(() => factory.CreateConfig(configName, radiusConfig, serviceConfig));
    }
    
    [Theory]
    [InlineData("none")]
    [InlineData("radius")]
    public void CreateClientConfiguration_ReplyAttributesNoLdapServer_ShouldThrow(string firstFactor)
    {
        var radiusConfig = new RadiusAdapterConfiguration()
        {
            AppSettings = new AppSettingsSection()
            {
                MultifactorApiUrl = "http://127.0.0.1",
                MultifactorNasIdentifier = "identifier",
                MultifactorSharedSecret = "secret",
                SignUpGroups = "groups",
                BypassSecondFactorWhenApiUnreachable = true,
                FirstFactorAuthenticationSource = firstFactor,
                RadiusSharedSecret = "secret",
                PrivacyMode = "Full",
                PreAuthenticationMethod = "None",
                AuthenticationCacheLifetime = "00:01:00",
                CallingStationIdAttribute = "12345",
                InvalidCredentialDelay = "3"
            },
            RadiusReply = new RadiusReplySection()
            {
                Attributes = new RadiusReplyAttributesSection(new RadiusReplyAttribute() { Name = "name", From = "attr" })
            }
        };

        var serviceConfig = new ServiceConfiguration();
        var configName = "name";
        var dictionaryMock = new Mock<IRadiusDictionary>();
        var attribute = new DictionaryAttribute("name", 1, "type");
        dictionaryMock.Setup(x => x.GetAttribute(It.IsAny<string>())).Returns(attribute);
        var factory = new ClientConfigurationFactory(dictionaryMock.Object);
        Assert.Throws<InvalidConfigurationException>(() => factory.CreateConfig(configName, radiusConfig, serviceConfig));
    }
    
    [Theory]
    [ClassData(typeof(EmptyStringsListInput))]
    public void CreateClientConfiguration_EmptyFirstFactor_ShouldThrow(string emptyString)
    {
        var radiusConfig = new RadiusAdapterConfiguration()
        {
            AppSettings = new AppSettingsSection()
            {
                MultifactorApiUrl = "http://127.0.0.1",
                MultifactorNasIdentifier = "identifier",
                MultifactorSharedSecret = "secret",
                SignUpGroups = "groups",
                BypassSecondFactorWhenApiUnreachable = true,
                FirstFactorAuthenticationSource = emptyString,
                RadiusSharedSecret = "secret",
                PrivacyMode = "Full",
                PreAuthenticationMethod = "None",
                AuthenticationCacheLifetime = "00:01:00",
                CallingStationIdAttribute = "12345",
                InvalidCredentialDelay = "3"
            }
        };

        var serviceConfig = new ServiceConfiguration();
        var configName = "name";
        var dictionaryMock = new Mock<IRadiusDictionary>();
        var attribute = new DictionaryAttribute("name", 1, "type");
        dictionaryMock.Setup(x => x.GetAttribute(It.IsAny<string>())).Returns(attribute);
        var factory =
            new ClientConfigurationFactory(dictionaryMock.Object);
        Assert.Throws<InvalidConfigurationException>(() => factory.CreateConfig(configName, radiusConfig, serviceConfig));
    }
    
    [Theory]
    [InlineData("123")]
    [InlineData("windows")]
    [InlineData("!2")]
    public void CreateClientConfiguration_InvalidFirstFactor_ShouldThrow(string emptyString)
    {
        var radiusConfig = new RadiusAdapterConfiguration()
        {
            AppSettings = new AppSettingsSection()
            {
                MultifactorApiUrl = "http://127.0.0.1",
                MultifactorNasIdentifier = "identifier",
                MultifactorSharedSecret = "secret",
                SignUpGroups = "groups",
                BypassSecondFactorWhenApiUnreachable = true,
                FirstFactorAuthenticationSource = emptyString,
                RadiusSharedSecret = "secret",
                PrivacyMode = "Full",
                PreAuthenticationMethod = "None",
                AuthenticationCacheLifetime = "00:01:00",
                CallingStationIdAttribute = "12345",
                InvalidCredentialDelay = "3"
            }
        };

        var serviceConfig = new ServiceConfiguration();
        var configName = "name";
        var dictionaryMock = new Mock<IRadiusDictionary>();
        var attribute = new DictionaryAttribute("name", 1, "type");
        dictionaryMock.Setup(x => x.GetAttribute(It.IsAny<string>())).Returns(attribute);
        var factory =
            new ClientConfigurationFactory(dictionaryMock.Object);
        Assert.Throws<InvalidConfigurationException>(() => factory.CreateConfig(configName, radiusConfig, serviceConfig));
    }
    
    [Theory]
    [ClassData(typeof(EmptyStringsListInput))]
    public void CreateClientConfiguration_EmptyRadiusSharedSecret_ShouldThrow(string emptyString)
    {
        var radiusConfig = new RadiusAdapterConfiguration()
        {
            AppSettings = new AppSettingsSection()
            {
                MultifactorApiUrl = "http://127.0.0.1",
                MultifactorNasIdentifier = "identifier",
                MultifactorSharedSecret = "secret",
                SignUpGroups = "groups",
                BypassSecondFactorWhenApiUnreachable = true,
                FirstFactorAuthenticationSource = "None",
                RadiusSharedSecret = emptyString,
                PrivacyMode = "Full",
                PreAuthenticationMethod = "None",
                AuthenticationCacheLifetime = "00:01:00",
                CallingStationIdAttribute = "12345",
                InvalidCredentialDelay = "3"
            }
        };

        var serviceConfig = new ServiceConfiguration();
        var configName = "name";
        var dictionaryMock = new Mock<IRadiusDictionary>();
        var attribute = new DictionaryAttribute("name", 1, "type");
        dictionaryMock.Setup(x => x.GetAttribute(It.IsAny<string>())).Returns(attribute);
        var factory =
            new ClientConfigurationFactory(dictionaryMock.Object);
        Assert.Throws<InvalidConfigurationException>(() => factory.CreateConfig(configName, radiusConfig, serviceConfig));
    }
    
    [Theory]
    [ClassData(typeof(EmptyStringsListInput))]
    public void CreateClientConfiguration_EmptyMultifactorNasIdentifier_ShouldThrow(string emptyString)
    {
        var radiusConfig = new RadiusAdapterConfiguration()
        {
            AppSettings = new AppSettingsSection()
            {
                MultifactorApiUrl = "http://127.0.0.1",
                MultifactorNasIdentifier = emptyString,
                MultifactorSharedSecret = "secret",
                SignUpGroups = "groups",
                BypassSecondFactorWhenApiUnreachable = true,
                FirstFactorAuthenticationSource = "None",
                RadiusSharedSecret = "secret",
                PrivacyMode = "Full",
                PreAuthenticationMethod = "None",
                AuthenticationCacheLifetime = "00:01:00",
                CallingStationIdAttribute = "12345",
                InvalidCredentialDelay = "3"
            }
        };

        var serviceConfig = new ServiceConfiguration();
        var configName = "name";
        var dictionaryMock = new Mock<IRadiusDictionary>();
        var attribute = new DictionaryAttribute("name", 1, "type");
        dictionaryMock.Setup(x => x.GetAttribute(It.IsAny<string>())).Returns(attribute);
        var factory =
            new ClientConfigurationFactory(dictionaryMock.Object);
        Assert.Throws<InvalidConfigurationException>(() => factory.CreateConfig(configName, radiusConfig, serviceConfig));
    }
    
    [Theory]
    [ClassData(typeof(EmptyStringsListInput))]
    public void CreateClientConfiguration_EmptyMultifactorSharedSecret_ShouldThrow(string emptyString)
    {
        var radiusConfig = new RadiusAdapterConfiguration()
        {
            AppSettings = new AppSettingsSection()
            {
                MultifactorApiUrl = "http://127.0.0.1",
                MultifactorNasIdentifier = "nasIdentifier",
                MultifactorSharedSecret = emptyString,
                SignUpGroups = "groups",
                BypassSecondFactorWhenApiUnreachable = true,
                FirstFactorAuthenticationSource = "None",
                RadiusSharedSecret = "secret",
                PrivacyMode = "Full",
                PreAuthenticationMethod = "None",
                AuthenticationCacheLifetime = "00:01:00",
                CallingStationIdAttribute = "12345",
                InvalidCredentialDelay = "3"
            }
        };

        var serviceConfig = new ServiceConfiguration();
        var configName = "name";
        var dictionaryMock = new Mock<IRadiusDictionary>();
        var attribute = new DictionaryAttribute("name", 1, "type");
        dictionaryMock.Setup(x => x.GetAttribute(It.IsAny<string>())).Returns(attribute);
        var factory =
            new ClientConfigurationFactory(dictionaryMock.Object);
        Assert.Throws<InvalidConfigurationException>(() => factory.CreateConfig(configName, radiusConfig, serviceConfig));
    }
    
    [Theory]
    [InlineData("123")]
    [InlineData("error")]
    public void CreateClientConfiguration_InvalidPrivacyMode_ShouldThrow(string privacyMode)
    {
        var radiusConfig = new RadiusAdapterConfiguration()
        {
            AppSettings = new AppSettingsSection()
            {
                MultifactorApiUrl = "http://127.0.0.1",
                MultifactorNasIdentifier = "nasIdentifier",
                MultifactorSharedSecret = "Secret",
                SignUpGroups = "groups",
                BypassSecondFactorWhenApiUnreachable = true,
                FirstFactorAuthenticationSource = "None",
                RadiusSharedSecret = "secret",
                PrivacyMode = privacyMode,
                PreAuthenticationMethod = "None",
                AuthenticationCacheLifetime = "00:01:00",
                CallingStationIdAttribute = "12345",
                InvalidCredentialDelay = "3"
            }
        };

        var serviceConfig = new ServiceConfiguration();
        var configName = "name";
        var dictionaryMock = new Mock<IRadiusDictionary>();
        var attribute = new DictionaryAttribute("name", 1, "type");
        dictionaryMock.Setup(x => x.GetAttribute(It.IsAny<string>())).Returns(attribute);
        var factory =
            new ClientConfigurationFactory(dictionaryMock.Object);
        Assert.Throws<InvalidConfigurationException>(() => factory.CreateConfig(configName, radiusConfig, serviceConfig));
    }
    
    [Theory]
    [InlineData("-1")]
    [InlineData("error")]
    [InlineData("1-2-6")]
    [InlineData("-1-1-6")]
    public void CreateClientConfiguration_InvalidCredentialDelay_ShouldThrow(string delay)
    {
        var radiusConfig = new RadiusAdapterConfiguration()
        {
            AppSettings = new AppSettingsSection()
            {
                MultifactorApiUrl = "http://127.0.0.1",
                MultifactorNasIdentifier = "nasIdentifier",
                MultifactorSharedSecret = "Secret",
                SignUpGroups = "groups",
                BypassSecondFactorWhenApiUnreachable = true,
                FirstFactorAuthenticationSource = "None",
                RadiusSharedSecret = "secret",
                PrivacyMode = "None",
                PreAuthenticationMethod = "None",
                AuthenticationCacheLifetime = "00:01:00",
                CallingStationIdAttribute = "12345",
                InvalidCredentialDelay = delay
            }
        };

        var serviceConfig = new ServiceConfiguration();
        var configName = "name";
        var dictionaryMock = new Mock<IRadiusDictionary>();
        var attribute = new DictionaryAttribute("name", 1, "type");
        dictionaryMock.Setup(x => x.GetAttribute(It.IsAny<string>())).Returns(attribute);
        var factory =
            new ClientConfigurationFactory(dictionaryMock.Object);
        Assert.Throws<InvalidConfigurationException>(() => factory.CreateConfig(configName, radiusConfig, serviceConfig));
    }
    
    [Theory]
    [InlineData("$")]
    [InlineData("?")]
    [InlineData("!")]
    public void CreateClientConfiguration_InvalidSignUpGroups_ShouldThrow(string groups)
    {
        var radiusConfig = new RadiusAdapterConfiguration()
        {
            AppSettings = new AppSettingsSection()
            {
                MultifactorApiUrl = "http://127.0.0.1",
                MultifactorNasIdentifier = "nasIdentifier",
                MultifactorSharedSecret = "Secret",
                SignUpGroups = groups,
                BypassSecondFactorWhenApiUnreachable = true,
                FirstFactorAuthenticationSource = "None",
                RadiusSharedSecret = "secret",
                PrivacyMode = "None",
                PreAuthenticationMethod = "None",
                AuthenticationCacheLifetime = "00:01:00",
                CallingStationIdAttribute = "12345",
                InvalidCredentialDelay = "1"
            }
        };

        var serviceConfig = new ServiceConfiguration();
        var configName = "name";
        var dictionaryMock = new Mock<IRadiusDictionary>();
        var attribute = new DictionaryAttribute("name", 1, "type");
        dictionaryMock.Setup(x => x.GetAttribute(It.IsAny<string>())).Returns(attribute);
        var factory =
            new ClientConfigurationFactory(dictionaryMock.Object);
        Assert.Throws<InvalidConfigurationException>(() => factory.CreateConfig(configName, radiusConfig, serviceConfig));
    }
    
    [Theory]
    [InlineData("$")]
    [InlineData("?")]
    [InlineData("!")]
    [InlineData("123")]
    [InlineData("aa")]
    [InlineData("00:00")]
    public void CreateClientConfiguration_InvalidAuthenticationCacheLifetime_ShouldThrow(string lifetime)
    {
        var radiusConfig = new RadiusAdapterConfiguration()
        {
            AppSettings = new AppSettingsSection()
            {
                MultifactorApiUrl = "http://127.0.0.1",
                MultifactorNasIdentifier = "nasIdentifier",
                MultifactorSharedSecret = "Secret",
                SignUpGroups = "group",
                BypassSecondFactorWhenApiUnreachable = true,
                FirstFactorAuthenticationSource = "None",
                RadiusSharedSecret = "secret",
                PrivacyMode = "None",
                PreAuthenticationMethod = "None",
                AuthenticationCacheLifetime = lifetime,
                CallingStationIdAttribute = "12345",
                InvalidCredentialDelay = "1"
            }
        };

        var serviceConfig = new ServiceConfiguration();
        var configName = "name";
        var dictionaryMock = new Mock<IRadiusDictionary>();
        var attribute = new DictionaryAttribute("name", 1, "type");
        dictionaryMock.Setup(x => x.GetAttribute(It.IsAny<string>())).Returns(attribute);
        var factory =
            new ClientConfigurationFactory(dictionaryMock.Object);
        Assert.Throws<InvalidConfigurationException>(() => factory.CreateConfig(configName, radiusConfig, serviceConfig));
    }

    [Fact]
    public void CreateClientConfiguration_SingleValidWhiteIp_ShouldCreate()
    {
        var whiteList = "127.0.0.1";
        var radiusConfig = new RadiusAdapterConfiguration()
        {
            AppSettings = new AppSettingsSection()
            {
                MultifactorApiUrl = "http://127.0.0.1",
                MultifactorNasIdentifier = "nasIdentifier",
                MultifactorSharedSecret = "Secret",
                SignUpGroups = "group",
                BypassSecondFactorWhenApiUnreachable = true,
                FirstFactorAuthenticationSource = "None",
                RadiusSharedSecret = "secret",
                PrivacyMode = "None",
                PreAuthenticationMethod = "None",
                CallingStationIdAttribute = "12345",
                InvalidCredentialDelay = "1",
                IpWhiteList = whiteList
            }
        };
        
        var serviceConfig = new ServiceConfiguration();
        var configName = "name";
        var dictionaryMock = new Mock<IRadiusDictionary>();
        var attribute = new DictionaryAttribute("name", 1, "type");
        dictionaryMock.Setup(x => x.GetAttribute(It.IsAny<string>())).Returns(attribute);
        var factory = new ClientConfigurationFactory(dictionaryMock.Object);
        var config = factory.CreateConfig(configName, radiusConfig, serviceConfig);
        
        var expectedWhiteList = IPAddressRange.Parse(whiteList);
        Assert.Equal(expectedWhiteList, config.IpWhiteList.First());
    }
    
    [Fact]
    public void CreateClientConfiguration_MultipleValidWhiteIps_ShouldCreate()
    {
        var whiteList = "127.0.0.1; 127.0.0.2-128.0.0.1; 127.2.0.0/16";
        var radiusConfig = new RadiusAdapterConfiguration()
        {
            AppSettings = new AppSettingsSection()
            {
                MultifactorApiUrl = "http://127.0.0.1",
                MultifactorNasIdentifier = "nasIdentifier",
                MultifactorSharedSecret = "Secret",
                SignUpGroups = "group",
                BypassSecondFactorWhenApiUnreachable = true,
                FirstFactorAuthenticationSource = "None",
                RadiusSharedSecret = "secret",
                PrivacyMode = "None",
                PreAuthenticationMethod = "None",
                CallingStationIdAttribute = "12345",
                InvalidCredentialDelay = "1",
                IpWhiteList = whiteList
            }
        };
        
        var serviceConfig = new ServiceConfiguration();
        var configName = "name";
        var dictionaryMock = new Mock<IRadiusDictionary>();
        var attribute = new DictionaryAttribute("name", 1, "type");
        dictionaryMock.Setup(x => x.GetAttribute(It.IsAny<string>())).Returns(attribute);
        var factory = new ClientConfigurationFactory(dictionaryMock.Object);
        var config = factory.CreateConfig(configName, radiusConfig, serviceConfig);
        
        var expectedWhiteList = new[] { IPAddressRange.Parse("127.0.0.1"), IPAddressRange.Parse("127.0.0.2-128.0.0.1"), IPAddressRange.Parse("127.2.0.0/16") };
        Assert.True(expectedWhiteList.SequenceEqual(config.IpWhiteList));
    }
    
    [Fact]
    public void CreateClientConfiguration_InvalidIpWhiteList_ShouldThrow()
    {
        var whiteList = "127.0.0.1; invalid-ip-address";
        var radiusConfig = new RadiusAdapterConfiguration()
        {
            AppSettings = new AppSettingsSection()
            {
                MultifactorApiUrl = "http://127.0.0.1",
                MultifactorNasIdentifier = "nasIdentifier",
                MultifactorSharedSecret = "Secret",
                SignUpGroups = "group",
                BypassSecondFactorWhenApiUnreachable = true,
                FirstFactorAuthenticationSource = "None",
                RadiusSharedSecret = "secret",
                PrivacyMode = "None",
                PreAuthenticationMethod = "None",
                CallingStationIdAttribute = "12345",
                InvalidCredentialDelay = "1",
                IpWhiteList = whiteList
            }
        };
        
        var serviceConfig = new ServiceConfiguration();
        var configName = "name";
        var dictionaryMock = new Mock<IRadiusDictionary>();
        var attribute = new DictionaryAttribute("name", 1, "type");
        dictionaryMock.Setup(x => x.GetAttribute(It.IsAny<string>())).Returns(attribute);
        var factory = new ClientConfigurationFactory(dictionaryMock.Object);
       Assert.Throws<InvalidConfigurationException>(() => factory.CreateConfig(configName, radiusConfig, serviceConfig));
    }
}