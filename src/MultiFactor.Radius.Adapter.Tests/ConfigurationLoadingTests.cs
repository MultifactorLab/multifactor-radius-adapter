using FluentAssertions;
using FluentAssertions.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MultiFactor.Radius.Adapter.Configuration.Core;
using MultiFactor.Radius.Adapter.Core.Exceptions;
using MultiFactor.Radius.Adapter.Tests.Fixtures;
using MultiFactor.Radius.Adapter.Tests.Fixtures.ConfigLoading;

namespace MultiFactor.Radius.Adapter.Tests;

public class ConfigurationLoadingTests
{
    [Fact]
    public void ReadConfiguration_ShouldReturnMultiConfig()
    {
        var host = TestHostFactory.CreateHost(services =>
        {
            services.RemoveService<IRootConfigurationProvider>().AddSingleton<IRootConfigurationProvider, TestRootConfigProvider>();
            services.RemoveService<IClientConfigurationsProvider>().AddSingleton<IClientConfigurationsProvider, TestClientConfigsProvider>();
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
    public void ReadConfiguration_ShouldReturnSingleConfig()
    {
        var host = TestHostFactory.CreateHost(services =>
        {
            services.RemoveService<IRootConfigurationProvider>().AddSingleton<IRootConfigurationProvider, TestRootConfigProvider>();
            services.RemoveService<IClientConfigurationsProvider>().AddSingleton<IClientConfigurationsProvider, TestClientConfigsProvider>();
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
    public void ReadConfiguration_SingleModeAndInvalidSettings_ShouldThrow(string asset, string msg)
    {
        var host = TestHostFactory.CreateHost(services =>
        {
            services.RemoveService<IRootConfigurationProvider>().AddSingleton<IRootConfigurationProvider, TestRootConfigProvider>();
            services.RemoveService<IClientConfigurationsProvider>().AddSingleton<IClientConfigurationsProvider, TestClientConfigsProvider>();
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
            var builder = Host.CreateApplicationBuilder();
            builder.ConfigureApplication(services =>
            {
                services.RemoveService<IRootConfigurationProvider>().AddSingleton<IRootConfigurationProvider, TestRootConfigProvider>();
                services.RemoveService<IClientConfigurationsProvider>().AddSingleton<IClientConfigurationsProvider, TestClientConfigsProvider>();
                services.Configure<TestConfigProviderOptions>(x =>
                {
                    x.RootConfigFilePath = TestEnvironment.GetAssetPath(asset);
                });
            });
            return builder.Build();
        };

        act.Should().Throw<InvalidConfigurationException>().WithMessage(msg);
    }
    
    [Theory]
    [InlineData("root-ffa-is-ad-and-empty-domain.config", "Configuration error: 'active-directory-domain' element not found")]
    [InlineData("root-wrong-load-active-directory-nested-groups.config", "Configuration error: Can't parse 'load-active-directory-nested-groups' value")]
    public void ReadConfiguration_SingleModeAndWrongADSettings_ShouldThrow(string asset, string msg)
    {
        var host = TestHostFactory.CreateHost(services =>
        {
            services.RemoveService<IRootConfigurationProvider>().AddSingleton<IRootConfigurationProvider, TestRootConfigProvider>();
            services.RemoveService<IClientConfigurationsProvider>().AddSingleton<IClientConfigurationsProvider, TestClientConfigsProvider>();
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
    public void ReadConfiguration_MultiModeAndInvalidSettings_ShouldThrow(string asset, string msg)
    {
        var host = TestHostFactory.CreateHost(services =>
        {
            services.RemoveService<IRootConfigurationProvider>().AddSingleton<IRootConfigurationProvider, TestRootConfigProvider>();
            services.RemoveService<IClientConfigurationsProvider>().AddSingleton<IClientConfigurationsProvider, TestClientConfigsProvider>();
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
}