using Moq;
using Multifactor.Radius.Adapter.v2.Core.Configuration.Client.Build;
using Multifactor.Radius.Adapter.v2.Core.Configuration.Service;
using Multifactor.Radius.Adapter.v2.Core.Radius.Attributes;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configuration.Exceptions;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configuration.RadiusAdapter;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configuration.RadiusAdapter.Sections;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configuration.RadiusAdapter.Sections.LdapServer;
using Multifactor.Radius.Adapter.v2.Tests.Fixture;

namespace Multifactor.Radius.Adapter.v2.Tests.ConfigurationTests.ClientConfigurationFactoryTests;

public class LdapSettingsTests
{
    [Fact]
    public void CreateClientConfiguration_ShouldReturnDefaultLdapServerConfiguration()
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
                AdapterClientEndpoint = "127.0.0.1",
                AdapterServerEndpoint = "127.0.0.1",
                RadiusSharedSecret = "secret",
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

        var serviceConfig = new ServiceConfiguration();
        var configName = "name";
        var dictionaryMock = new Mock<IRadiusDictionary>();
        var attribute = new DictionaryAttribute("name", 1, "type");
        dictionaryMock.Setup(x => x.GetAttribute(It.IsAny<string>())).Returns(attribute);
        var factory =
            new ClientConfigurationFactory(dictionaryMock.Object);
        var clientConfig = factory.CreateConfig(configName, radiusConfig, serviceConfig);
        Assert.NotNull(clientConfig);
        Assert.NotNull(clientConfig.LdapServers);
        Assert.NotEmpty(clientConfig.LdapServers);
        var config = clientConfig.LdapServers[0];

        Assert.Equal("connectionString", config.ConnectionString);
        Assert.Equal("username", config.UserName);
        Assert.Equal("password", config.Password);

        Assert.Empty(config.AccessGroups);
        Assert.Empty(config.SecondFaGroups);
        Assert.Empty(config.SecondFaBypassGroups);
        Assert.Empty(config.NestedGroupsBaseDns);
        Assert.Empty(config.PhoneAttributes);
        Assert.True(config.LoadNestedGroups);
        Assert.True(string.IsNullOrWhiteSpace(config.IdentityAttribute));
        Assert.Equal(30, config.BindTimeoutInSeconds);
    }

    [Fact]
    public void CreateClientConfiguration_ShouldReturnSingleLdapServerConfiguration()
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
                AdapterClientEndpoint = "127.0.0.1",
                AdapterServerEndpoint = "127.0.0.1",
                RadiusSharedSecret = "secret",
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
                        AccessGroups = "groups",
                        SecondFaGroups = "second fa groups",
                        SecondFaBypassGroups = "second fa bypass groups",
                        LoadNestedGroups = true,
                        NestedGroupsBaseDn = "nested groups",
                        PhoneAttributes = "phone attributes",
                        IdentityAttribute = "Id"
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

        var serverConfig = clientConfig.LdapServers.First();

        Assert.Equal("connectionString", serverConfig.ConnectionString);
        Assert.Equal("username", serverConfig.UserName);
        Assert.Equal("password", serverConfig.Password);
        Assert.Collection(serverConfig.AccessGroups, e => Assert.Equal("groups", e));
        Assert.Collection(serverConfig.SecondFaGroups, e => Assert.Equal("second fa groups", e));
        Assert.Collection(serverConfig.SecondFaBypassGroups, e => Assert.Equal("second fa bypass groups", e));
        Assert.Collection(serverConfig.NestedGroupsBaseDns, e => Assert.Equal("nested groups", e));
        Assert.Collection(serverConfig.PhoneAttributes, e => Assert.Equal("phone attributes", e));
        Assert.True(serverConfig.LoadNestedGroups);
        Assert.Equal("Id", serverConfig.IdentityAttribute);
    }

    [Fact]
    public void CreateClientConfiguration_ShouldReturnTwoLdapServerConfigurations()
    {
        var ldapConfig = new LdapServerConfiguration()
        {
            ConnectionString = "connectionString",
            UserName = "username",
            Password = "password",
            AccessGroups = "groups",
            SecondFaGroups = "second fa groups",
            SecondFaBypassGroups = "second fa bypass groups",
            LoadNestedGroups = true,
            NestedGroupsBaseDn = "nested groups",
            PhoneAttributes = "phone attributes",
            IdentityAttribute = "Id"
        };

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
                AdapterClientEndpoint = "127.0.0.1",
                AdapterServerEndpoint = "127.0.0.1",
                RadiusSharedSecret = "secret",
                InvalidCredentialDelay = "3",
            },
            LdapServers = new LdapServersSection()
            {
                LdapServers = new[]
                {
                    ldapConfig,
                    ldapConfig
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
        Assert.Equal(2, clientConfig.LdapServers.Count);
        foreach (var serverConfig in clientConfig.LdapServers)
        {
            Assert.Equal("connectionString", serverConfig.ConnectionString);
            Assert.Equal("username", serverConfig.UserName);
            Assert.Equal("password", serverConfig.Password);
            Assert.Collection(serverConfig.AccessGroups, e => Assert.Equal("groups", e));
            Assert.Collection(serverConfig.SecondFaGroups, e => Assert.Equal("second fa groups", e));
            Assert.Collection(serverConfig.SecondFaBypassGroups, e => Assert.Equal("second fa bypass groups", e));
            Assert.Collection(serverConfig.NestedGroupsBaseDns, e => Assert.Equal("nested groups", e));
            Assert.Collection(serverConfig.PhoneAttributes, e => Assert.Equal("phone attributes", e));
            Assert.True(serverConfig.LoadNestedGroups);
            Assert.Equal("Id", serverConfig.IdentityAttribute);
        }
    }

    [Theory]
    [InlineData("Ldap")]
    public void CreateClientConfiguration_NoServerConfigs_ShouldThrow(string factor)
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
                FirstFactorAuthenticationSource = factor,
                AdapterClientEndpoint = "127.0.0.1",
                AdapterServerEndpoint = "127.0.0.1",
                RadiusSharedSecret = "secret",
                InvalidCredentialDelay = "3",
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
    public void CreateClientConfiguration_EmptyConnectionString_ShouldThrow(string connection)
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
                AdapterClientEndpoint = "127.0.0.1",
                AdapterServerEndpoint = "127.0.0.1",
                RadiusSharedSecret = "secret",
                InvalidCredentialDelay = "3",
                NpsServerEndpoint = "127.0.0.1",
            },
            LdapServers = new LdapServersSection()
            {
                LdapServers = new[]
                {
                    new LdapServerConfiguration()
                    {
                        ConnectionString = connection,
                        UserName = "username",
                        Password = "password"
                    }
                }
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
    public void CreateClientConfiguration_EmptyUserName_ShouldThrow(string userName)
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
                AdapterClientEndpoint = "127.0.0.1",
                AdapterServerEndpoint = "127.0.0.1",
                RadiusSharedSecret = "secret",
                InvalidCredentialDelay = "3",
                NpsServerEndpoint = "127.0.0.1",
            },
            LdapServers = new LdapServersSection()
            {
                LdapServers = new[]
                {
                    new LdapServerConfiguration()
                    {
                        ConnectionString = "connection",
                        UserName = userName,
                        Password = "password"
                    }
                }
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
    public void CreateClientConfiguration_EmptyPassword_ShouldThrow(string password)
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
                AdapterClientEndpoint = "127.0.0.1",
                AdapterServerEndpoint = "127.0.0.1",
                RadiusSharedSecret = "secret",
                InvalidCredentialDelay = "3",
                NpsServerEndpoint = "127.0.0.1",
            },
            LdapServers = new LdapServersSection()
            {
                LdapServers = new[]
                {
                    new LdapServerConfiguration()
                    {
                        ConnectionString = "connection",
                        UserName = "userName",
                        Password = password
                    }
                }
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
    
    [Fact]
    public void CreateClientConfiguration_SimultaneousUseOfDomainRules_ShouldThrow()
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
                AdapterClientEndpoint = "127.0.0.1",
                AdapterServerEndpoint = "127.0.0.1",
                RadiusSharedSecret = "secret",
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
                        IncludedDomains = "included domains",
                        ExcludedDomains = "excluded domains",
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
        Assert.Throws<InvalidConfigurationException>(() => factory.CreateConfig(configName, radiusConfig, serviceConfig));
    }
    
    [Fact]
    public void CreateClientConfiguration_SimultaneousUseOfSuffixRules_ShouldThrow()
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
                AdapterClientEndpoint = "127.0.0.1",
                AdapterServerEndpoint = "127.0.0.1",
                RadiusSharedSecret = "secret",
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
                        IncludedSuffixes = "included suffixes",
                        ExcludedSuffixes = "excluded suffixes",
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
        Assert.Throws<InvalidConfigurationException>(() => factory.CreateConfig(configName, radiusConfig, serviceConfig));
    }
    
    [Fact]
    public void CreateClientConfiguration_EnableTrustedDomainsAndNoUpnRequirements_ShouldThrow()
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
                AdapterClientEndpoint = "127.0.0.1",
                AdapterServerEndpoint = "127.0.0.1",
                RadiusSharedSecret = "secret",
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
                        RequiresUpn = false,
                        EnableTrustedDomains = true
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
        Assert.Throws<InvalidConfigurationException>(() => factory.CreateConfig(configName, radiusConfig, serviceConfig));
    }
    
    [Fact]
    public void CreateClientConfiguration_IncludedDomains_ShouldSet()
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
                AdapterClientEndpoint = "127.0.0.1",
                AdapterServerEndpoint = "127.0.0.1",
                RadiusSharedSecret = "secret",
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
                        IncludedDomains = "included domains"
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
        var config = factory.CreateConfig(configName, radiusConfig, serviceConfig);
        var serverConfig = config.LdapServers.First();
        Assert.NotNull(serverConfig.DomainPermissions);
        Assert.Collection(serverConfig.DomainPermissions.IncludedValues, e => Assert.Equal("included domains", e));
    }
    
    [Fact]
    public void CreateClientConfiguration_ExcludedDomains_ShouldSet()
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
                AdapterClientEndpoint = "127.0.0.1",
                AdapterServerEndpoint = "127.0.0.1",
                RadiusSharedSecret = "secret",
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
                        ExcludedDomains = "excluded domains"
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
        var config = factory.CreateConfig(configName, radiusConfig, serviceConfig);
        var serverConfig = config.LdapServers.First();
        Assert.NotNull(serverConfig.DomainPermissions);
        Assert.Collection(serverConfig.DomainPermissions.ExcludedValues, e => Assert.Equal("excluded domains", e));
    }
    
    [Fact]
    public void CreateClientConfiguration_IncludedSuffixes_ShouldSet()
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
                AdapterClientEndpoint = "127.0.0.1",
                AdapterServerEndpoint = "127.0.0.1",
                RadiusSharedSecret = "secret",
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
                        IncludedSuffixes = "included suffixes"
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
        var config = factory.CreateConfig(configName, radiusConfig, serviceConfig);
        var serverConfig = config.LdapServers.First();
        Assert.NotNull(serverConfig.DomainPermissions);
        Assert.Collection(serverConfig.SuffixesPermissions.IncludedValues, e => Assert.Equal("included suffixes", e));
    }
    
    [Fact]
    public void CreateClientConfiguration_ExcludedSuffixes_ShouldSet()
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
                AdapterClientEndpoint = "127.0.0.1",
                AdapterServerEndpoint = "127.0.0.1",
                RadiusSharedSecret = "secret",
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
                        ExcludedSuffixes = "excluded suffixes"
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
        var config = factory.CreateConfig(configName, radiusConfig, serviceConfig);
        var serverConfig = config.LdapServers.First();
        Assert.NotNull(serverConfig.DomainPermissions);
        Assert.Collection(serverConfig.SuffixesPermissions.ExcludedValues, e => Assert.Equal("excluded suffixes", e));
    }
}