using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MultiFactor.Radius.Adapter.Configuration.Core;
using MultiFactor.Radius.Adapter.Configuration.Features.PreAuthModeFeature;
using MultiFactor.Radius.Adapter.Core;
using MultiFactor.Radius.Adapter.Core.Exceptions;
using MultiFactor.Radius.Adapter.Tests.Fixtures;
using MultiFactor.Radius.Adapter.Tests.Fixtures.ConfigLoading;

namespace MultiFactor.Radius.Adapter.Tests.AdapterConfig;

[Trait("Category", "Adapter Configuration")]
public partial class ConfigurationLoadingTests
{
    [Fact]
    [Trait("Category", "Base Config Loading")]
    public void ShouldReturnMultiConfig()
    {
        var host = TestHostFactory.CreateHost(services =>
        {
            services.Configure<TestConfigProviderOptions>(x =>
            {
                x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-multi.config");
                x.ClientConfigFilePaths = new[]
                {
                    TestEnvironment.GetAssetPath(TestAssetLocation.ClientsDirectory, "client-minimal.config")
                };
            });
        });

        var conf = host.Services.GetRequiredService<IServiceConfiguration>();

        conf.Should().NotBeNull();
        conf.SingleClientMode.Should().BeFalse();
        conf.Clients.Should().NotBeNullOrEmpty().And.ContainSingle(x => x.Name == "client-minimal");
    }

    [Fact]
    [Trait("Category", "Base Config Loading")]
    public void ShouldReturnSingleConfig()
    {
        var host = TestHostFactory.CreateHost(services =>
        {
            services.Configure<TestConfigProviderOptions>(x =>
            {
                x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-single.config");
            });
        });

        var conf = host.Services.GetRequiredService<IServiceConfiguration>();
        conf.Should().NotBeNull();
        conf.SingleClientMode.Should().BeTrue();
        conf.Clients.Should().NotBeEmpty().And.ContainSingle(x => x.Name == "General");
    }

    [Theory]
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
        var host = TestHostFactory.CreateHost(services =>
        {
            services.Configure<TestConfigProviderOptions>(x =>
            {
                x.RootConfigFilePath = TestEnvironment.GetAssetPath(asset);
            });
        });

        var act = () => host.Services.GetRequiredService<IServiceConfiguration>();

        act.Should().Throw<InvalidConfigurationException>().WithMessage(msg);
    }

    [Theory]
    [InlineData("root-empty-logging-level.config", "Configuration error: 'logging-level' element not found")]
    public void CreateHost_InvalidSettings_ShouldThrow(string asset, string msg)
    {
        var act = () =>
        {
            var builder = RadiusHost.CreateApplicationBuilder();
            builder.Configure(host =>
            {
                host.ConfigureApplication(services =>
                {
                    services.Configure<TestConfigProviderOptions>(x =>
                    {
                        x.RootConfigFilePath = TestEnvironment.GetAssetPath(asset);
                    });
                });
            });
            return builder.Build();
        };

        act.Should().Throw<InvalidConfigurationException>().WithMessage(msg);
    }

    [Theory]
    [InlineData("root-ffa-is-ad-and-empty-domain.config", "Configuration error: 'active-directory-domain' element not found")]
    [InlineData("root-wrong-load-active-directory-nested-groups.config", "Configuration error: Can't parse 'load-active-directory-nested-groups' value")]
    public void SingleModeAndWrongADSettings_ShouldThrow(string asset, string msg)
    {
        var host = TestHostFactory.CreateHost(services =>
        {
            services.Configure<TestConfigProviderOptions>(x =>
            {
                x.RootConfigFilePath = TestEnvironment.GetAssetPath(asset);
            });
        });

        var act = () => host.Services.GetRequiredService<IServiceConfiguration>();

        act.Should().Throw<InvalidConfigurationException>().WithMessage(msg);
    }

    [Theory]
    [InlineData("client-empty-identifier-and-ip.config", "Configuration error: Either 'radius-client-nas-identifier' or 'radius-client-ip' must be configured")]
    public void MultiModeAndInvalidSettings_ShouldThrow(string asset, string msg)
    {
        var host = TestHostFactory.CreateHost(services =>
        {
            services.Configure<TestConfigProviderOptions>(x =>
            {
                x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-multi.config");
                x.ClientConfigFilePaths = new[]
                {
                    TestEnvironment.GetAssetPath(TestAssetLocation.ClientsDirectory, asset)
                };
            });
        });

        var act = () => host.Services.GetRequiredService<IServiceConfiguration>();

        act.Should().Throw<InvalidConfigurationException>().WithMessage(msg);
    }

    [Fact]
    [Trait("Category", "Invalid Credential Delay")]
    public void SingleModeAndZeroCredDelay_ShouldReturnZeroConfig()
    {
        var host = TestHostFactory.CreateHost(services =>
        {
            services.Configure<TestConfigProviderOptions>(x =>
            {
                x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-valid-credential-delay-0.config");
            });
        });

        var config = host.Services.GetRequiredService<IServiceConfiguration>();
        var waiterConfig = config.InvalidCredentialDelay;

        Assert.NotNull(waiterConfig);
        Assert.True(waiterConfig.ZeroDelay);
        Assert.Equal(0, waiterConfig.Min);
        Assert.Equal(0, waiterConfig.Max);
    }

    [Fact]
    [Trait("Category", "Invalid Credential Delay")]
    public void SingleModeAndRangeCredDelay_ShouldReturnConfig()
    {
        var host = TestHostFactory.CreateHost(services =>
        {
            services.Configure<TestConfigProviderOptions>(x =>
            {
                x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-valid-credential-delay-1-2.config");
            });
        });

        var config = host.Services.GetRequiredService<IServiceConfiguration>();
        var waiterConfig = config.InvalidCredentialDelay;

        Assert.NotNull(waiterConfig);
        Assert.False(waiterConfig.ZeroDelay);
        Assert.Equal(1, waiterConfig.Min);
        Assert.Equal(2, waiterConfig.Max);
    }

    [Fact]
    [Trait("Category", "Invalid Credential Delay")]
    public void MultiModeAndZeroCredDelay_ShouldReturnZeroConfig()
    {
        var host = TestHostFactory.CreateHost(services =>
        {
            services.Configure<TestConfigProviderOptions>(x =>
            {
                x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-multi.config");
                x.ClientConfigFilePaths = new[]
                {
                    TestEnvironment.GetAssetPath(TestAssetLocation.ClientsDirectory, "client-cred-delay-0.config")
                };
            });
        });

        var conf = host.Services.GetRequiredService<IServiceConfiguration>();

        var cli = Assert.Single(conf.Clients, x => x.Name == "client-cred-delay-0");
        var waiterConfig = cli.InvalidCredentialDelay;

        Assert.NotNull(waiterConfig);
        Assert.True(waiterConfig.ZeroDelay);
        Assert.Equal(0, waiterConfig.Min);
        Assert.Equal(0, waiterConfig.Max);
    }

    [Fact]
    [Trait("Category", "Invalid Credential Delay")]
    public void MultiModeAndRangeCredDelay_ShouldReturnConfig()
    {
        var host = TestHostFactory.CreateHost(services =>
        {
            services.Configure<TestConfigProviderOptions>(x =>
            {
                x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-multi.config");
                x.ClientConfigFilePaths = new[]
                {
                    TestEnvironment.GetAssetPath(TestAssetLocation.ClientsDirectory, "client-cred-delay-1-2.config")
                };
            });
        });

        var conf = host.Services.GetRequiredService<IServiceConfiguration>();

        var cli = Assert.Single(conf.Clients, x => x.Name == "client-cred-delay-1-2");
        var waiterConfig = cli.InvalidCredentialDelay;

        Assert.NotNull(waiterConfig);
        Assert.False(waiterConfig.ZeroDelay);
        Assert.Equal(1, waiterConfig.Min);
        Assert.Equal(2, waiterConfig.Max);
    }
    
    [Fact]
    [Trait("Category", "Invalid Credential Delay")]
    public void MultiModeAndRangeCredDelay_ShouldOverrideRootConfig()
    {
        var host = TestHostFactory.CreateHost(services =>
        {
            services.Configure<TestConfigProviderOptions>(x =>
            {
                x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-multi-credential-delay-1-2.config");
                x.ClientConfigFilePaths = new[]
                {
                    TestEnvironment.GetAssetPath(TestAssetLocation.ClientsDirectory, "client-cred-delay-3.config")
                };
            });
        });

        var conf = host.Services.GetRequiredService<IServiceConfiguration>();

        Assert.Equal(1, conf.InvalidCredentialDelay.Min);
        Assert.Equal(2, conf.InvalidCredentialDelay.Max);

        var cli = Assert.Single(conf.Clients, x => x.Name == "client-cred-delay-3");
        var waiterConfig = cli.InvalidCredentialDelay;

        Assert.Equal(3, waiterConfig.Min);
        Assert.Equal(3, waiterConfig.Max);
    }
    
    [Fact]
    [Trait("Category", "Pre Authentication Method")]
    public void MultiPreAuthMethodAndNoCredentialDelay_ShouldFail()
    {
        var host = TestHostFactory.CreateHost(services =>
        {
            services.Configure<TestConfigProviderOptions>(x =>
            {
                x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-multi.config");
                x.ClientConfigFilePaths = new[]
                {
                    TestEnvironment.GetAssetPath(TestAssetLocation.ClientsDirectory, "pre-auth-method/client-pre-auth-method-otp-with-no-cred-delay.config")
                };
            });
        });

        var act = () => host.Services.GetRequiredService<IServiceConfiguration>();

        act.Should().Throw<InvalidConfigurationException>().WithMessage("Configuration error: to enable pre-auth second factor for this client please set 'invalid-credential-delay' min value to 2 or more");
    }
    
    [Fact]
    [Trait("Category", "Pre Authentication Method")]
    public void MultiPreAuthMethodNoneAndNoCredentialDelay_ShouldSuccess()
    {
        var host = TestHostFactory.CreateHost(services =>
        {
            services.Configure<TestConfigProviderOptions>(x =>
            {
                x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-multi.config");
                x.ClientConfigFilePaths = new[]
                {
                    TestEnvironment.GetAssetPath(TestAssetLocation.ClientsDirectory, "pre-auth-method/client-pre-auth-method-none.config")
                };
            });
        });

        var conf = host.Services.GetRequiredService<IServiceConfiguration>();
        var cli = conf.Clients.First();

        var mode = cli.PreAuthnMode;
        Assert.NotNull(mode);
        Assert.Equal(PreAuthMode.None, mode.Mode);
    }
    
    [Theory]
    [Trait("Category", "Pre Authentication Method")]
    [InlineData("pre-auth-method/client-pre-auth-method-otp.config", PreAuthMode.Otp)]
    [InlineData("pre-auth-method/client-pre-auth-method-push.config", PreAuthMode.Push)]
    [InlineData("pre-auth-method/client-pre-auth-method-telegram.config", PreAuthMode.Telegram)]
    public void MultiPreAuthMethodWithCredDelay_ShouldSuccess(string asset, PreAuthMode mode)
    {
        var host = TestHostFactory.CreateHost(services =>
        {
            services.Configure<TestConfigProviderOptions>(x =>
            {
                x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-multi.config");
                x.ClientConfigFilePaths = new[]
                {
                    TestEnvironment.GetAssetPath(TestAssetLocation.ClientsDirectory, asset)
                };
            });
        });

        var conf = host.Services.GetRequiredService<IServiceConfiguration>();
        var cli = conf.Clients.First();

        Assert.Equal(mode, cli.PreAuthnMode.Mode);
    }
    
    [Fact]
    [Trait("Category", "Pre Authentication Method")]
    public void MultiAnyPreAuthMethodWithRootCredDelay_ShouldSuccess()
    {
        var host = TestHostFactory.CreateHost(services =>
        {
            services.Configure<TestConfigProviderOptions>(x =>
            {
                x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-multi-credential-delay-2-3.config");
                x.ClientConfigFilePaths = new[]
                {
                    TestEnvironment.GetAssetPath(TestAssetLocation.ClientsDirectory, "pre-auth-method/client-pre-auth-method-otp-with-no-cred-delay.config")
                };
            });
        });

        var conf = host.Services.GetRequiredService<IServiceConfiguration>();
        var cli = conf.Clients.First();

        Assert.Equal(PreAuthMode.Otp, cli.PreAuthnMode.Mode);
    }
}