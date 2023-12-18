using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration.Exceptions;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration.Features.AuthenticatedClientCacheFeature;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration.Features.PreAuthModeFeature;
using MultiFactor.Radius.Adapter.Tests.Fixtures;
using MultiFactor.Radius.Adapter.Tests.Fixtures.ConfigLoading;
using System.Net;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration.RootLevel;

namespace MultiFactor.Radius.Adapter.Tests.AdapterConfig;

[Trait("Category", "Adapter Configuration")]
public partial class ConfigurationLoadingTests
{
    [Fact]
    [Trait("Category", "Multi Config Loading")]
    public void ShouldReturnMultiConfig()
    {
        var host = TestHostFactory.CreateHost(builder =>
        {
            builder.Services.Configure<TestConfigProviderOptions>(x =>
            {
                x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-multi.config");
                x.ClientConfigFilePaths = new[]
                {
                    TestEnvironment.GetAssetPath(TestAssetLocation.ClientsDirectory, "client-minimal.config")
                };
            });
        });

        var conf = host.Service<IServiceConfiguration>();

        conf.Should().NotBeNull();
        conf.SingleClientMode.Should().BeFalse();
        conf.Clients.Should().NotBeNullOrEmpty().And.ContainSingle(x => x.Name == "client-minimal");
    }

    [Fact]
    [Trait("Category", "Single Config Loading")]
    public void ShouldReturnSingleConfig()
    {
        var host = TestHostFactory.CreateHost(builder =>
        {
            builder.Services.Configure<TestConfigProviderOptions>(x =>
            {
                x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-single.config");
            });
        });

        var conf = host.Service<IServiceConfiguration>();
        conf.Should().NotBeNull();
        conf.SingleClientMode.Should().BeTrue();
        conf.Clients.Should().NotBeEmpty().And.ContainSingle(x => x.Name == "multifactor-radius-adapter.dll");
    }

    [Theory]
    [Trait("Category", "adapter-server-endpoint")]
    [Trait("Category", "multifactor-api-url")]
    [Trait("Category", "multifactor-nas-identifier")]
    [Trait("Category", "multifactor-shared-secret")]
    [Trait("Category", "invalid-credential-delay")]
    [Trait("Category", "first-factor-authentication-source")]
    [Trait("Category", "privacy-mode")]
    [InlineData("root-empty-adapter-server-endpoint.config", 
        "Configuration error: 'adapter-server-endpoint' element not found. Config name: 'multifactor-radius-adapter.dll'")]
    [InlineData("root-wrong-adapter-server-endpoint.config", 
        "Configuration error: Can't parse 'adapter-server-endpoint' value. Config name: 'multifactor-radius-adapter.dll'")]
    [InlineData("root-empty-multifactor-api-url.config", 
        "Configuration error: 'multifactor-api-url' element not found. Config name: 'multifactor-radius-adapter.dll'")]
    [InlineData("root-empty-multifactor-nas-identifier.config", 
        "Configuration error: 'multifactor-nas-identifier' element not found. Config name: 'multifactor-radius-adapter.dll'")]
    [InlineData("root-empty-multifactor-shared-secret.config", 
        "Configuration error: 'multifactor-shared-secret' element not found. Config name: 'multifactor-radius-adapter.dll'")]
    [InlineData("root-empty-first-factor-authentication-source.config", 
        "Configuration error: 'first-factor-authentication-source' element not found. Config name: 'multifactor-radius-adapter.dll'")]
    [InlineData("root-first-factor-authentication-source-is-digit.config", 
        "Configuration error: Can't parse 'first-factor-authentication-source' value. Must be one of: ActiveDirectory, Radius, None. Config name: 'multifactor-radius-adapter.dll'")]
    [InlineData("root-first-factor-authentication-source-is-invalid.config", 
        "Configuration error: Can't parse 'first-factor-authentication-source' value. Must be one of: ActiveDirectory, Radius, None. Config name: 'multifactor-radius-adapter.dll'")]
    [InlineData("root-wrong-invalid-credential-delay.config", 
        "Configuration error: Can't parse 'invalid-credential-delay' value. Config name: 'multifactor-radius-adapter.dll'")]
    [InlineData("root-wrong-privacy-mode.config", 
        "Configuration error: Can't parse 'privacy-mode' value. Must be one of: Full, None, Partial:Field1,Field2. Config name: 'multifactor-radius-adapter.dll'")]
    public void SingleModeAndInvalidSettings_ShouldThrow(string asset, string msg)
    {
        var host = TestHostFactory.CreateHost(builder =>
        {
            builder.Services.Configure<TestConfigProviderOptions>(x =>
            {
                x.RootConfigFilePath = TestEnvironment.GetAssetPath(asset);
            });
        });

        var act = () => host.Service<IServiceConfiguration>();

        act.Should().Throw<InvalidConfigurationException>().WithMessage(msg);
    }

    [Fact]
    [Trait("Category", "active-directory-domain")]
    public void SingleModeAndWrongADSettings_ShouldThrow()
    {
        var host = TestHostFactory.CreateHost(builder =>
        {
            builder.Services.Configure<TestConfigProviderOptions>(x =>
            {
                x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-ffa-is-ad-and-empty-domain.config");
            });
        });

        var act = () => host.Service<IServiceConfiguration>();

        act.Should().Throw<InvalidConfigurationException>()
            .WithMessage("Configuration error: 'active-directory-domain' element not found. Config name: 'multifactor-radius-adapter.dll'");
    }

    [Fact]
    [Trait("Category", "radius-client-nas-identifier")]
    public void MultiModeAndInvalidSettings_ShouldThrow()
    {
        var host = TestHostFactory.CreateHost(builder =>
        {
            builder.Services.Configure<TestConfigProviderOptions>(x =>
            {
                x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-multi.config");
                x.ClientConfigFilePaths = new[]
                {
                    TestEnvironment.GetAssetPath(TestAssetLocation.ClientsDirectory, "client-empty-identifier-and-ip.config")
                };
            });
        });

        var act = () => host.Service<IServiceConfiguration>();

        act.Should().Throw<InvalidConfigurationException>()
            .WithMessage("Configuration error: Either 'radius-client-nas-identifier' or 'radius-client-ip' must be configured. Config name: 'client-empty-identifier-and-ip'");
    }

    [Fact]
    [Trait("Category", "invalid-credential-delay")]
    public void SingleModeAndZeroCredDelay_ShouldReturnZeroConfig()
    {
        var host = TestHostFactory.CreateHost(builder =>
        {
            builder.Services.Configure<TestConfigProviderOptions>(x =>
            {
                x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-valid-credential-delay-0.config");
            });
        });

        var config = host.Service<IServiceConfiguration>();
        var waiterConfig = config.InvalidCredentialDelay;

        Assert.NotNull(waiterConfig);
        Assert.True(waiterConfig.ZeroDelay);
        Assert.Equal(0, waiterConfig.Min);
        Assert.Equal(0, waiterConfig.Max);
    }

    [Fact]
    [Trait("Category", "invalid-credential-delay")]
    public void SingleModeAndRangeCredDelay_ShouldReturnConfig()
    {
        var host = TestHostFactory.CreateHost(builder =>
        {
            builder.Services.Configure<TestConfigProviderOptions>(x =>
            {
                x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-valid-credential-delay-1-2.config");
            });
        });

        var config = host.Service<IServiceConfiguration>();
        var waiterConfig = config.InvalidCredentialDelay;

        Assert.NotNull(waiterConfig);
        Assert.False(waiterConfig.ZeroDelay);
        Assert.Equal(1, waiterConfig.Min);
        Assert.Equal(2, waiterConfig.Max);
    }

    [Fact]
    [Trait("Category", "invalid-credential-delay")]
    public void MultiModeAndZeroCredDelay_ShouldReturnZeroConfig()
    {
        var host = TestHostFactory.CreateHost(builder =>
        {
            builder.Services.Configure<TestConfigProviderOptions>(x =>
            {
                x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-multi.config");
                x.ClientConfigFilePaths = new[]
                {
                    TestEnvironment.GetAssetPath(TestAssetLocation.ClientsDirectory, "client-cred-delay-0.config")
                };
            });
        });

        var conf = host.Service<IServiceConfiguration>();

        var cli = Assert.Single(conf.Clients, x => x.Name == "client-cred-delay-0");
        var waiterConfig = cli.InvalidCredentialDelay;

        Assert.NotNull(waiterConfig);
        Assert.True(waiterConfig.ZeroDelay);
        Assert.Equal(0, waiterConfig.Min);
        Assert.Equal(0, waiterConfig.Max);
    }

    [Fact]
    [Trait("Category", "invalid-credential-delay")]
    public void MultiModeAndRangeCredDelay_ShouldReturnConfig()
    {
        var host = TestHostFactory.CreateHost(builder =>
        {
            builder.Services.Configure<TestConfigProviderOptions>(x =>
            {
                x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-multi.config");
                x.ClientConfigFilePaths = new[]
                {
                    TestEnvironment.GetAssetPath(TestAssetLocation.ClientsDirectory, "client-cred-delay-1-2.config")
                };
            });
        });

        var conf = host.Service<IServiceConfiguration>();

        var cli = Assert.Single(conf.Clients, x => x.Name == "client-cred-delay-1-2");
        var waiterConfig = cli.InvalidCredentialDelay;

        Assert.NotNull(waiterConfig);
        Assert.False(waiterConfig.ZeroDelay);
        Assert.Equal(1, waiterConfig.Min);
        Assert.Equal(2, waiterConfig.Max);
    }
    
    [Fact]
    [Trait("Category", "invalid-credential-delay")]
    public void MultiModeAndRangeCredDelay_ShouldOverrideRootConfig()
    {
        var host = TestHostFactory.CreateHost(builder =>
        {
            builder.Services.Configure<TestConfigProviderOptions>(x =>
            {
                x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-multi-credential-delay-1-2.config");
                x.ClientConfigFilePaths = new[]
                {
                    TestEnvironment.GetAssetPath(TestAssetLocation.ClientsDirectory, "client-cred-delay-3.config")
                };
            });
        });

        var conf = host.Service<IServiceConfiguration>();

        Assert.Equal(1, conf.InvalidCredentialDelay.Min);
        Assert.Equal(2, conf.InvalidCredentialDelay.Max);

        var cli = Assert.Single(conf.Clients, x => x.Name == "client-cred-delay-3");
        var waiterConfig = cli.InvalidCredentialDelay;

        Assert.Equal(3, waiterConfig.Min);
        Assert.Equal(3, waiterConfig.Max);
    }
    
    [Fact]
    [Trait("Category", "pre-authentication-method")]
    public void MultiPreAuthMethodAndNoCredentialDelay_ShouldFail()
    {
        var host = TestHostFactory.CreateHost(builder =>
        {
            builder.Services.Configure<TestConfigProviderOptions>(x =>
            {
                x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-multi.config");
                x.ClientConfigFilePaths = new[]
                {
                    TestEnvironment.GetAssetPath(TestAssetLocation.ClientsDirectory, "pre-auth-method/client-pre-auth-method-otp-with-no-cred-delay.config")
                };
            });
        });

        var act = () => host.Service<IServiceConfiguration>();

        act.Should().Throw<InvalidConfigurationException>().WithMessage("Configuration error: to enable pre-auth second factor for this client please set 'invalid-credential-delay' min value to 2 or more. Config name: 'client-pre-auth-method-otp-with-no-cred-delay'");
    }
    
    [Fact]
    [Trait("Category", "pre-authentication-method")]
    public void MultiPreAuthMethodNoneAndNoCredentialDelay_ShouldSuccess()
    {
        var host = TestHostFactory.CreateHost(builder =>
        {
            builder.Services.Configure<TestConfigProviderOptions>(x =>
            {
                x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-multi.config");
                x.ClientConfigFilePaths = new[]
                {
                    TestEnvironment.GetAssetPath(TestAssetLocation.ClientsDirectory, "pre-auth-method/client-pre-auth-method-none.config")
                };
            });
        });

        var conf = host.Service<IServiceConfiguration>();
        var cli = conf.Clients.First();

        var mode = cli.PreAuthnMode;
        Assert.NotNull(mode);
        Assert.Equal(PreAuthMode.None, mode.Mode);
    }
    
    [Theory]
    [Trait("Category", "pre-authentication-method")]
    [InlineData("pre-auth-method/client-pre-auth-method-otp.config", PreAuthMode.Otp)]
    [InlineData("pre-auth-method/client-pre-auth-method-push.config", PreAuthMode.Push)]
    [InlineData("pre-auth-method/client-pre-auth-method-telegram.config", PreAuthMode.Telegram)]
    public void MultiPreAuthMethodWithCredDelay_ShouldSuccess(string asset, PreAuthMode mode)
    {
        var host = TestHostFactory.CreateHost(builder =>
        {
            builder.Services.Configure<TestConfigProviderOptions>(x =>
            {
                x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-multi.config");
                x.ClientConfigFilePaths = new[]
                {
                    TestEnvironment.GetAssetPath(TestAssetLocation.ClientsDirectory, asset)
                };
            });
        });

        var conf = host.Service<IServiceConfiguration>();
        var cli = conf.Clients.First();

        Assert.Equal(mode, cli.PreAuthnMode.Mode);
    }
    
    [Fact]
    [Trait("Category", "pre-authentication-method")]
    public void MultiAnyPreAuthMethodWithRootCredDelay_ShouldSuccess()
    {
        var host = TestHostFactory.CreateHost(builder =>
        {
            builder.Services.Configure<TestConfigProviderOptions>(x =>
            {
                x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-multi-credential-delay-2-3.config");
                x.ClientConfigFilePaths = new[]
                {
                    TestEnvironment.GetAssetPath(TestAssetLocation.ClientsDirectory, "pre-auth-method/client-pre-auth-method-otp-with-no-cred-delay.config")
                };
            });
        });

        var conf = host.Service<IServiceConfiguration>();
        var cli = conf.Clients.First();

        Assert.Equal(PreAuthMode.Otp, cli.PreAuthnMode.Mode);
    }
    
    [Theory]
    [Trait("Category", "use-upn-as-identity")]
    [Trait("Category", "use-attribute-as-identity")]
    [InlineData("client-identity-attr-with-use-upn-as-identity-true.config")]
    [InlineData("client-identity-attr-with-use-upn-as-identity-false.config")]
    public void Multi_BothUseUpnAsIdentityAndIdentityAttrSpecified_ShouldFail(string cliConf)
    {
        var host = TestHostFactory.CreateHost(builder =>
        {
            builder.Services.Configure<TestConfigProviderOptions>(x =>
            {
                x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-multi.config");
                x.ClientConfigFilePaths = new[]
                {
                    TestEnvironment.GetAssetPath(TestAssetLocation.ClientsDirectory, cliConf)
                };
            });
        });

        var act = () => host.Service<IServiceConfiguration>();

        act.Should().Throw<InvalidConfigurationException>()
            .WithMessage($"Configuration error: Using settings 'use-upn-as-identity' and 'use-attribute-as-identity' together is unacceptable. Prefer using 'use-attribute-as-identity'. Config name: '{Path.GetFileNameWithoutExtension(cliConf)}'");
    }
    
    [Fact]
    [Trait("Category", "use-attribute-as-identity")]
    public void Multi_IdentityAttrSpecifiedOnly_ShouldSuccess()
    {
        var host = TestHostFactory.CreateHost(builder =>
        {
            builder.Services.Configure<TestConfigProviderOptions>(x =>
            {
                x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-multi.config");
                x.ClientConfigFilePaths = new[]
                {
                    TestEnvironment.GetAssetPath(TestAssetLocation.ClientsDirectory, "client-identity-attr-without-use-upn-as-identity.config")
                };
            });
        });

        var conf = host.Service<IServiceConfiguration>();
        var cli = conf.Clients.First();

        Assert.Equal("attr", cli.TwoFAIdentityAttribute);
    }
    
    [Theory]
    [Trait("Category", "use-upn-as-identity")]
    [InlineData("client-use-upn-as-identity-only-true.config", "userPrincipalName")]
    [InlineData("client-use-upn-as-identity-only-false.config", null)]
    public void Multi_UeUpnAsIdentitySpecifiedOnly_ShouldSuccess(string cliConf, string attr)
    {
        var host = TestHostFactory.CreateHost(builder =>
        {
            builder.Services.Configure<TestConfigProviderOptions>(x =>
            {
                x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-multi.config");
                x.ClientConfigFilePaths = new[]
                {
                    TestEnvironment.GetAssetPath(TestAssetLocation.ClientsDirectory, cliConf)
                };
            });
        });

        var conf = host.Service<IServiceConfiguration>();
        var cli = conf.Clients.First();

        Assert.Equal(attr, cli.TwoFAIdentityAttribute);
    }

    [Fact]
    [Trait("Category", "multifactor-api-timeout")]
    public void Single_ApiTimeout_ShouldSuccess()
    {
        var host = TestHostFactory.CreateHost(builder =>
        {
            builder.Services.Configure<TestConfigProviderOptions>(x =>
            {
                x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-single-multifactor-api-timeout-valid.config");
            });
        });

        var conf = host.Service<IServiceConfiguration>();

        Assert.Equal(TimeSpan.FromSeconds(125), conf.ApiTimeout);
    }
    
    [Fact]
    [Trait("Category", "multifactor-api-timeout")]
    public void Single_ApiTimeoutInvalid_ShouldSetDefault()
    {
        var host = TestHostFactory.CreateHost(builder =>
        {
            builder.Services.Configure<TestConfigProviderOptions>(x =>
            {
                x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-single-multifactor-api-timeout-invalid.config");
            });
        });

        var conf = host.Service<IServiceConfiguration>();

        Assert.Equal(TimeSpan.FromSeconds(65), conf.ApiTimeout);
    }
    
    [Fact]
    [Trait("Category", "multifactor-api-timeout")]
    public void Single_ApiTimeoutIsNotSpecified_ShouldSetDefault()
    {
        var host = TestHostFactory.CreateHost(builder =>
        {
            builder.Services.Configure<TestConfigProviderOptions>(x =>
            {
                x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-single.config");
            });
        });

        var conf = host.Service<IServiceConfiguration>();

        Assert.Equal(TimeSpan.FromSeconds(65), conf.ApiTimeout);
    }
    
    [Fact]
    [Trait("Category", "multifactor-api-timeout")]
    public void Single_ApiTimeoutLessThanMinimal_ShouldSetDefault()
    {
        var host = TestHostFactory.CreateHost(builder =>
        {
            builder.Services.Configure<TestConfigProviderOptions>(x =>
            {
                x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-single-multifactor-api-timeout-less-than-min.config");
            });
        });

        var conf = host.Service<IServiceConfiguration>();

        Assert.Equal(TimeSpan.FromSeconds(65), conf.ApiTimeout);
    }
    
    [Fact]
    [Trait("Category", "multifactor-api-timeout")]
    public void Single_ApiTimeoutZero_ShouldSetInfinity()
    {
        var host = TestHostFactory.CreateHost(builder =>
        {
            builder.Services.Configure<TestConfigProviderOptions>(x =>
            {
                x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-single-multifactor-api-timeout-zero.config");
            });
        });

        var conf = host.Service<IServiceConfiguration>();

        Assert.Equal(Timeout.InfiniteTimeSpan, conf.ApiTimeout);
    }
    
    [Fact]
    [Trait("Category", "authentication-cache-lifetime")]
    public void Multi_AuthCacheLifetime_ShouldSuccess()
    {
        var host = TestHostFactory.CreateHost(builder =>
        {
            builder.Services.Configure<TestConfigProviderOptions>(x =>
            {
                x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-multi.config");
                x.ClientConfigFilePaths = new[]
                {
                    TestEnvironment.GetAssetPath(TestAssetLocation.ClientsDirectory, "authentication-cache-lifetime.config")
                };
            });
        });

        var conf = host.Service<IServiceConfiguration>();
        var cacheConfig = conf.Clients.First().AuthenticationCacheLifetime;

        Assert.Equal(TimeSpan.FromSeconds(12), cacheConfig.Lifetime);
        Assert.True(cacheConfig.Enabled);
    }
    
    [Fact]
    [Trait("Category", "authentication-cache-lifetime")]
    public void Multi_AuthCacheLifetimeInvalid_ShouldThrow()
    {
        var host = TestHostFactory.CreateHost(builder =>
        {
            builder.Services.Configure<TestConfigProviderOptions>(x =>
            {
                x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-multi.config");
                x.ClientConfigFilePaths = new[]
                {
                    TestEnvironment.GetAssetPath(TestAssetLocation.ClientsDirectory, "authentication-cache-lifetime-invalid.config")
                };
            });
        });

        var action = () => host.Service<IServiceConfiguration>();

        var ex = Assert.Throws<InvalidConfigurationException>(action);
        Assert.Equal("Configuration error: Can't parse 'authentication-cache-lifetime' value. Config name: 'authentication-cache-lifetime-invalid'", ex.Message);
    }

    [Fact]
    [Trait("Category", "authentication-cache-lifetime")]
    public void Multi_AuthCacheLifetimeZero_ShouldSetZero()
    {
        var host = TestHostFactory.CreateHost(builder =>
        {
            builder.Services.Configure<TestConfigProviderOptions>(x =>
            {
                x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-multi.config");
                x.ClientConfigFilePaths = new[]
                {
                    TestEnvironment.GetAssetPath(TestAssetLocation.ClientsDirectory, "authentication-cache-lifetime-zero.config")
                };
            });
        });

        var conf = host.Service<IServiceConfiguration>();
        var cacheConfig = conf.Clients.First().AuthenticationCacheLifetime;

        Assert.Equal(TimeSpan.Zero, cacheConfig.Lifetime);
        Assert.False(cacheConfig.Enabled);
    }
    
    [Fact]
    [Trait("Category", "authentication-cache-lifetime")]
    public void Multi_AuthCacheLifetimeNotSpecified_ShouldSetDefault()
    {
        var host = TestHostFactory.CreateHost(builder =>
        {
            builder.Services.Configure<TestConfigProviderOptions>(x =>
            {
                x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-multi.config");
                x.ClientConfigFilePaths = new[]
                {
                    TestEnvironment.GetAssetPath(TestAssetLocation.ClientsDirectory, "client-minimal.config")
                };
            });
        });

        var conf = host.Service<IServiceConfiguration>();
        var cacheConfig = conf.Clients.First().AuthenticationCacheLifetime;

        Assert.Equal(AuthenticatedClientCacheConfig.Default, cacheConfig);
    }
    
    [Fact]
    [Trait("Category", "authentication-cache-minimal-matching")]
    public void Multi_AuthCacheLifetimeMinimalMatchingNotSpecified_ShouldSetFalse()
    {
        var host = TestHostFactory.CreateHost(builder =>
        {
            builder.Services.Configure<TestConfigProviderOptions>(x =>
            {
                x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-multi.config");
                x.ClientConfigFilePaths = new[]
                {
                    TestEnvironment.GetAssetPath(TestAssetLocation.ClientsDirectory, "authentication-cache-lifetime.config")
                };
            });
        });

        var conf = host.Service<IServiceConfiguration>();
        var cacheConfig = conf.Clients.First().AuthenticationCacheLifetime;

        Assert.False(cacheConfig.MinimalMatching);
    }
    
    [Fact]
    [Trait("Category", "authentication-cache-minimal-matching")]
    public void Multi_AuthCacheLifetimeMinimalMatchingFalse_ShouldSetFalse()
    {
        var host = TestHostFactory.CreateHost(builder =>
        {
            builder.Services.Configure<TestConfigProviderOptions>(x =>
            {
                x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-multi.config");
                x.ClientConfigFilePaths = new[]
                {
                    TestEnvironment.GetAssetPath(TestAssetLocation.ClientsDirectory, "authentication-cache-minimal-matching-false.config")
                };
            });
        });

        var conf = host.Service<IServiceConfiguration>();
        var cacheConfig = conf.Clients.First().AuthenticationCacheLifetime;

        Assert.False(cacheConfig.MinimalMatching);
    }
    
    [Fact]
    [Trait("Category", "authentication-cache-minimal-matching")]
    public void Multi_AuthCacheLifetimeMinimalMatchingTrue_ShouldSetFalse()
    {
        var host = TestHostFactory.CreateHost(builder =>
        {
            builder.Services.Configure<TestConfigProviderOptions>(x =>
            {
                x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-multi.config");
                x.ClientConfigFilePaths = new[]
                {
                    TestEnvironment.GetAssetPath(TestAssetLocation.ClientsDirectory, "authentication-cache-minimal-matching-true.config")
                };
            });
        });

        var conf = host.Service<IServiceConfiguration>();
        var cacheConfig = conf.Clients.First().AuthenticationCacheLifetime;

        Assert.True(cacheConfig.MinimalMatching);
    }
    
    [Fact]
    [Trait("Category", "ldap-bind-dn")]
    public void Multi_LdapBindDn_ShouldSet()
    {
        var host = TestHostFactory.CreateHost(builder =>
        {
            builder.Services.Configure<TestConfigProviderOptions>(x =>
            {
                x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-multi.config");
                x.ClientConfigFilePaths = new[]
                {
                    TestEnvironment.GetAssetPath(TestAssetLocation.ClientsDirectory, "other-settings.config")
                };
            });
        });

        var conf = host.Service<IServiceConfiguration>();
        Assert.Equal("cn=cn,dc=dc", conf.Clients.First().LdapBindDn);
    }
    
    [Fact]
    [Trait("Category", "service-account-user")]
    public void Multi_ServiceAccountUser_ShouldSet()
    {
        var host = TestHostFactory.CreateHost(builder =>
        {
            builder.Services.Configure<TestConfigProviderOptions>(x =>
            {
                x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-multi.config");
                x.ClientConfigFilePaths = new[]
                {
                    TestEnvironment.GetAssetPath(TestAssetLocation.ClientsDirectory, "other-settings.config")
                };
            });
        });

        var conf = host.Service<IServiceConfiguration>();
        Assert.Equal("user", conf.Clients.First().ServiceAccountUser);
    }
    
    [Fact]
    [Trait("Category", "service-account-password")]
    public void Multi_ServiceAccountPassword_ShouldSet()
    {
        var host = TestHostFactory.CreateHost(builder =>
        {
            builder.Services.Configure<TestConfigProviderOptions>(x =>
            {
                x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-multi.config");
                x.ClientConfigFilePaths = new[]
                {
                    TestEnvironment.GetAssetPath(TestAssetLocation.ClientsDirectory, "other-settings.config")
                };
            });
        });

        var conf = host.Service<IServiceConfiguration>();
        Assert.Equal("password", conf.Clients.First().ServiceAccountPassword);
    }
    
    [Fact]
    [Trait("Category", "phone-attribute")]
    public void Multi_PhoneAttribute_ShouldSet()
    {
        var host = TestHostFactory.CreateHost(builder =>
        {
            builder.Services.Configure<TestConfigProviderOptions>(x =>
            {
                x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-multi.config");
                x.ClientConfigFilePaths = new[]
                {
                    TestEnvironment.GetAssetPath(TestAssetLocation.ClientsDirectory, "phone-attribute.config")
                };  
            });
        });

        var conf = host.Service<IServiceConfiguration>();
        var cli = conf.Clients.First();
        Assert.Equal(2, cli.PhoneAttributes.Length);
        Assert.Equivalent(cli.PhoneAttributes, new[] { "mobilephone", "mobilephone2" });
    }   
    
    [Fact]
    [Trait("Category", "load-active-directory-nested-groups")]
    public void Multi_LoadNestedGroups_ShouldSet()
    {
        var host = TestHostFactory.CreateHost(builder =>
        {
            builder.Services.Configure<TestConfigProviderOptions>(x =>
            {
                x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-multi.config");
                x.ClientConfigFilePaths = new[]
                {
                    TestEnvironment.GetAssetPath(TestAssetLocation.ClientsDirectory, "other-settings.config")
                };  
            });
        });

        var conf = host.Service<IServiceConfiguration>();
        var cli = conf.Clients.First();
        Assert.True(cli.LoadActiveDirectoryNestedGroups);
    }
    
    [Fact]
    [Trait("Category", "use-active-directory-mobile-user-phone")]
    public void Multi_UseMobileUserPhone_ShouldSet()
    {
        var host = TestHostFactory.CreateHost(builder =>
        {
            builder.Services.Configure<TestConfigProviderOptions>(x =>
            {
                x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-multi.config");
                x.ClientConfigFilePaths = new[]
                {
                    TestEnvironment.GetAssetPath(TestAssetLocation.ClientsDirectory, "other-settings.config")
                };  
            });
        });

        var conf = host.Service<IServiceConfiguration>();
        var cli = conf.Clients.First();
        Assert.Single(cli.PhoneAttributes, x => x == "mobile");
    }
    
    [Fact]
    [Trait("Category", "use-active-directory-user-phone")]
    public void Multi_UseUserPhone_ShouldSet()
    {
        var host = TestHostFactory.CreateHost(builder =>
        {
            builder.Services.Configure<TestConfigProviderOptions>(x =>
            {
                x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-multi.config");
                x.ClientConfigFilePaths = new[]
                {
                    TestEnvironment.GetAssetPath(TestAssetLocation.ClientsDirectory, "other-settings.config")
                };  
            });
        });

        var conf = host.Service<IServiceConfiguration>();
        var cli = conf.Clients.First();
        Assert.Single(cli.PhoneAttributes, x => x == "telephoneNumber");
    }
    
    [Fact]
    [Trait("Category", "radius-client-ip")]
    public void Multi_RadiusClientIp_ShouldAddClients()
    {
        var host = TestHostFactory.CreateHost(builder =>
        {
            builder.Services.Configure<TestConfigProviderOptions>(x =>
            {
                x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-multi.config");
                x.ClientConfigFilePaths = new[]
                {
                    TestEnvironment.GetAssetPath(TestAssetLocation.ClientsDirectory, "radius-client-ip-without-nas-id.config")
                };  
            });
        });

        var conf = host.Service<IServiceConfiguration>();

        var cli = conf.GetClient(IPAddress.Parse("10.10.10.10"));
        Assert.Equal("radius-client-ip-without-nas-id", cli.Name);
        
        var cli1 = conf.GetClient(IPAddress.Parse("11.11.11.11"));
        Assert.Equal("radius-client-ip-without-nas-id", cli1.Name);
    }


    [Fact]
    public void LoadActiveDirectoryFirstFactorWithLdapBindDN_ShouldThrow()
    {
        var host = TestHostFactory.CreateHost(builder =>
        {
            builder.Services.Configure<TestConfigProviderOptions>(x =>
            {
                x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-multi.config");
                x.ClientConfigFilePaths = new[]
                {
                    TestEnvironment.GetAssetPath(TestAssetLocation.ClientsDirectory, "client-ldap-bind-dn-with-ad.config")
                };
            });
        });

        var act = () => host.Service<IServiceConfiguration>();

        act.Should().Throw<InvalidConfigurationException>().WithMessage("Configuration error: " +
            $"'ldap-bind-dn' shouldn't be used in combination with 'first-factor-authentication-source' == {AuthenticationSource.ActiveDirectory}");
    }

    [Fact]
    public void LoadNotActiveDirectoryFirstFactorWithLdapBindDN_ShouldSuccess()
    {
        var host = TestHostFactory.CreateHost(builder =>
        {
            builder.Services.Configure<TestConfigProviderOptions>(x =>
            {
                x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-multi.config");
                x.ClientConfigFilePaths = new[]
                {
                    TestEnvironment.GetAssetPath(TestAssetLocation.ClientsDirectory, "client-ldap-bind-dn-with-ldap.config")
                };
            });
        });

        var conf = host.Service<IServiceConfiguration>();
        var cli = conf.Clients.First();
        cli.LdapBindDn.Should().NotBeNull();
    }


    [Theory]
    [MemberData(nameof(UsernameTransformationRuleTestCases.TestCase1), MemberType = typeof(UsernameTransformationRuleTestCases))]
    public void ReadConfiguration_ShouldReadUsernameTransformationRules(UsernameTransformationRuleTestCase data)
    {
        var host = TestHostFactory.CreateHost(builder => {
            builder.Services
                .RemoveService<IRootConfigurationProvider>()
                .AddSingleton<IRootConfigurationProvider, TestRootConfigProvider>();
            builder.Services
                .RemoveService<IClientConfigurationsProvider>()
                .AddSingleton<IClientConfigurationsProvider, TestClientConfigsProvider>();

            builder.Services.Configure<TestConfigProviderOptions>(x =>
            {
                x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-multi.config");
                x.ClientConfigFilePaths = new[]
                {
                    TestEnvironment.GetAssetPath(TestAssetLocation.ClientsDirectory, data.Asset)
                };
            });
        });
        var act = host.Service<IServiceConfiguration>();
        act.Should().NotBeNull();
        act.Clients.Should()
            .NotBeNull()
            .And.HaveCountGreaterThan(0);

        act.Clients[0].UserNameTransformRules.BeforeFirstFactor.Should()
            .NotBeNullOrEmpty()
            .And
            .ContainSingle(x => x.Element.Replace == data.ReplaceFirst && x.Element.Match == data.MatchFirst);

        act.Clients[0].UserNameTransformRules.BeforeSecondFactor.Should()
            .NotBeNullOrEmpty()
            .And
            .ContainSingle(x => x.Element.Replace == data.ReplaceSecond && x.Element.Match == data.MatchSecond);
    }
}