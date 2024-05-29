using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using MultiFactor.Radius.Adapter.Core.Framework;
using MultiFactor.Radius.Adapter.Extensions;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration.Exceptions;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration.Features.AuthenticatedClientCacheFeature;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration.Features.PreAuthModeFeature;
using MultiFactor.Radius.Adapter.Tests.Fixtures;
using MultiFactor.Radius.Adapter.Tests.Fixtures.ConfigLoading;
using System.Net;

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
        conf.Clients.Should().NotBeEmpty().And.ContainSingle(x => x.Name == "General");
    }

    [Theory]
    [Trait("Category", "adapter-server-endpoint")]
    [Trait("Category", "multifactor-api-url")]
    [Trait("Category", "multifactor-nas-identifier")]
    [Trait("Category", "multifactor-shared-secret")]
    [Trait("Category", "invalid-credential-delay")]
    [Trait("Category", "first-factor-authentication-source")]
    [Trait("Category", "privacy-mode")]
    [InlineData("root-empty-adapter-server-endpoint.config", "Configuration error: 'adapter-server-endpoint' element not found")]
    [InlineData("root-wrong-adapter-server-endpoint.config", "Configuration error: Can't parse 'adapter-server-endpoint' value")]
    [InlineData("root-empty-multifactor-api-url.config", "Configuration error: 'multifactor-api-url' element not found")]
    [InlineData("root-empty-multifactor-nas-identifier.config", "Configuration error: 'multifactor-nas-identifier' element not found")]
    [InlineData("root-empty-multifactor-shared-secret.config", "Configuration error: 'multifactor-shared-secret' element not found")]
    [InlineData("root-empty-first-factor-authentication-source.config", "Configuration error: No clients' config files found. Use one of the *.template files in the /clients folder to customize settings. Then save this file as *.config.")]
    [InlineData("root-first-factor-authentication-source-is-digit.config", "Configuration error: Can't parse 'first-factor-authentication-source' value. Must be one of: ActiveDirectory, Radius, None")]
    [InlineData("root-first-factor-authentication-source-is-invalid.config", "Configuration error: Can't parse 'first-factor-authentication-source' value. Must be one of: ActiveDirectory, Radius, None")]
    [InlineData("root-wrong-invalid-credential-delay.config", "Configuration error: Can't parse 'invalid-credential-delay' value")]
    [InlineData("root-wrong-privacy-mode.config", "Configuration error: Can't parse 'privacy-mode' value. Must be one of: Full, None, Partial:Field1,Field2")]
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

    [Theory]
    [Trait("Category", "logging-level")]
    [InlineData("root-empty-logging-level.config", "Configuration error: 'logging-level' element not found")]
    public void CreateHost_InvalidLoggingSettings_ShouldThrow(string asset, string msg)
    {
        var host = TestHostFactory.CreateHost(builder =>
        {
            builder.Services.Configure<TestConfigProviderOptions>(x =>
            {
                x.RootConfigFilePath = TestEnvironment.GetAssetPath(asset);
            });
            builder.AddLogging();

        });

        var act = () => host.Service<IServiceConfiguration>();

        act.Should().Throw<InvalidConfigurationException>().WithMessage(msg);
    }

    [Theory]
    [Trait("Category", "active-directory-domain")]
    [InlineData("root-ffa-is-ad-and-empty-domain.config", "Configuration error: 'active-directory-domain' element not found")]
    [InlineData("root-wrong-load-active-directory-nested-groups.config", "Configuration error: Can't parse 'load-active-directory-nested-groups' value")]
    public void SingleModeAndWrongADSettings_ShouldThrow(string asset, string msg)
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

    [Theory]
    [Trait("Category", "radius-client-nas-identifier")]
    [InlineData("client-empty-identifier-and-ip.config", "Configuration error: Either 'radius-client-nas-identifier' or 'radius-client-ip' must be configured")]
    public void MultiModeAndInvalidSettings_ShouldThrow(string asset, string msg)
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

        var act = () => host.Service<IServiceConfiguration>();

        act.Should().Throw<InvalidConfigurationException>().WithMessage(msg);
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

        act.Should().Throw<InvalidConfigurationException>().WithMessage("Configuration error: to enable pre-auth second factor for this client please set 'invalid-credential-delay' min value to 2 or more");
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

        act.Should().Throw<InvalidConfigurationException>().WithMessage("Configuration error: Using settings 'use-upn-as-identity' and 'use-attribute-as-identity' together is unacceptable. Prefer using 'use-attribute-as-identity'.");
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
        Assert.Equal("Configuration error: Can't parse 'authentication-cache-lifetime' value", ex.Message);
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
    public void Multi_AuthCacheLifetimeMinimalMatchingInvalid_ShouldThrow()
    {
        var host = TestHostFactory.CreateHost(builder =>
        {
            builder.Services.Configure<TestConfigProviderOptions>(x =>
            {
                x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-multi.config");
                x.ClientConfigFilePaths = new[]
                {
                    TestEnvironment.GetAssetPath(TestAssetLocation.ClientsDirectory, "authentication-cache-minimal-matching-invalid.config")
                };
            });
        });

        var action = () => host.Service<IServiceConfiguration>();

        var ex = Assert.Throws<InvalidConfigurationException>(action);
        Assert.Equal("Configuration error: Can't parse 'authentication-cache-minimal-matching' value", ex.Message);
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
}