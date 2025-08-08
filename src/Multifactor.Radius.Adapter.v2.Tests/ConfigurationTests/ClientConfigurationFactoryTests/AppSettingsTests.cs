using System.Net;
using Moq;
using Multifactor.Radius.Adapter.v2.Core;
using Multifactor.Radius.Adapter.v2.Core.Auth;
using Multifactor.Radius.Adapter.v2.Core.Configuration.Client.Build;
using Multifactor.Radius.Adapter.v2.Core.Configuration.Service;
using Multifactor.Radius.Adapter.v2.Core.Radius.Attributes;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configuration.Exceptions;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configuration.RadiusAdapter;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configuration.RadiusAdapter.Sections;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configuration.RadiusAdapter.Sections.LdapServer;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configuration.RadiusAdapter.Sections.RadiusReply;
using Multifactor.Radius.Adapter.v2.Tests.Fixture;

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
        var nps = clientConfig.NpsServerEndpoints[0];
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
                AuthenticationCacheMinimalMatching = true,
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
    [InlineData("127.0.0.1")]
    [InlineData("127.0.0.1; 127.0.0.2")]
    [InlineData("127.0.0.1; 127.0.0.2; 127.0.0.3")]
    public void CreateClientConfiguration_MultipleNpsServers_ShouldReturnConfiguration(string npsServers)
    {
        var expectedNpsServers = Utils.SplitString(npsServers).Select(IPEndPoint.Parse);
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
                NpsServerEndpoint = npsServers,
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
        Assert.True(expectedNpsServers.SequenceEqual(clientConfig.NpsServerEndpoints));
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
                MultifactorNasIdentifier = "identifier",
                MultifactorSharedSecret = "secret",
                SignUpGroups = "groups",
                BypassSecondFactorWhenApiUnreachable = true,
                FirstFactorAuthenticationSource = firstFactor,
                RadiusSharedSecret = "secret",
                PrivacyMode = "Full",
                PreAuthenticationMethod = "None",
                AuthenticationCacheLifetime = "00:01:00",
                AuthenticationCacheMinimalMatching = true,
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
                MultifactorNasIdentifier = "identifier",
                MultifactorSharedSecret = "secret",
                SignUpGroups = "groups",
                BypassSecondFactorWhenApiUnreachable = true,
                FirstFactorAuthenticationSource = emptyString,
                RadiusSharedSecret = "secret",
                PrivacyMode = "Full",
                PreAuthenticationMethod = "None",
                AuthenticationCacheLifetime = "00:01:00",
                AuthenticationCacheMinimalMatching = true,
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
                MultifactorNasIdentifier = "identifier",
                MultifactorSharedSecret = "secret",
                SignUpGroups = "groups",
                BypassSecondFactorWhenApiUnreachable = true,
                FirstFactorAuthenticationSource = emptyString,
                RadiusSharedSecret = "secret",
                PrivacyMode = "Full",
                PreAuthenticationMethod = "None",
                AuthenticationCacheLifetime = "00:01:00",
                AuthenticationCacheMinimalMatching = true,
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
                MultifactorNasIdentifier = "identifier",
                MultifactorSharedSecret = "secret",
                SignUpGroups = "groups",
                BypassSecondFactorWhenApiUnreachable = true,
                FirstFactorAuthenticationSource = "None",
                RadiusSharedSecret = emptyString,
                PrivacyMode = "Full",
                PreAuthenticationMethod = "None",
                AuthenticationCacheLifetime = "00:01:00",
                AuthenticationCacheMinimalMatching = true,
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
                MultifactorNasIdentifier = emptyString,
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
                MultifactorNasIdentifier = "nasIdentifier",
                MultifactorSharedSecret = emptyString,
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
                MultifactorNasIdentifier = "nasIdentifier",
                MultifactorSharedSecret = "Secret",
                SignUpGroups = "groups",
                BypassSecondFactorWhenApiUnreachable = true,
                FirstFactorAuthenticationSource = "None",
                RadiusSharedSecret = "secret",
                PrivacyMode = privacyMode,
                PreAuthenticationMethod = "None",
                AuthenticationCacheLifetime = "00:01:00",
                AuthenticationCacheMinimalMatching = true,
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
                MultifactorNasIdentifier = "nasIdentifier",
                MultifactorSharedSecret = "Secret",
                SignUpGroups = "groups",
                BypassSecondFactorWhenApiUnreachable = true,
                FirstFactorAuthenticationSource = "None",
                RadiusSharedSecret = "secret",
                PrivacyMode = "None",
                PreAuthenticationMethod = "None",
                AuthenticationCacheLifetime = "00:01:00",
                AuthenticationCacheMinimalMatching = true,
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
                MultifactorNasIdentifier = "nasIdentifier",
                MultifactorSharedSecret = "Secret",
                SignUpGroups = groups,
                BypassSecondFactorWhenApiUnreachable = true,
                FirstFactorAuthenticationSource = "None",
                RadiusSharedSecret = "secret",
                PrivacyMode = "None",
                PreAuthenticationMethod = "None",
                AuthenticationCacheLifetime = "00:01:00",
                AuthenticationCacheMinimalMatching = true,
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
                MultifactorNasIdentifier = "nasIdentifier",
                MultifactorSharedSecret = "Secret",
                SignUpGroups = "group",
                BypassSecondFactorWhenApiUnreachable = true,
                FirstFactorAuthenticationSource = "None",
                RadiusSharedSecret = "secret",
                PrivacyMode = "None",
                PreAuthenticationMethod = "None",
                AuthenticationCacheLifetime = lifetime,
                AuthenticationCacheMinimalMatching = true,
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
}