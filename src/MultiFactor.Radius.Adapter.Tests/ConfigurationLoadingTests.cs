using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using MultiFactor.Radius.Adapter.Configuration.ConfigurationLoading;
using MultiFactor.Radius.Adapter.Configuration.Core;
using MultiFactor.Radius.Adapter.Tests.Fixtures;
using MultiFactor.Radius.Adapter.Tests.Fixtures.ConfigLoading;

namespace MultiFactor.Radius.Adapter.Tests
{

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
                    x.RootConfigFilePath = TestEnvironment.GetAssetPath("default-root.config");
                    x.ClientConfigsFolderPath = TestEnvironment.GetAssetPath(TestAssetLocation.ClientsDirectory);
                });
            });

            var conf = host.Services.GetRequiredService<IServiceConfiguration>();

            conf.Should().NotBeNull();
            conf.SingleClientMode.Should().BeFalse();
            conf.Clients.Should().NotBeNullOrEmpty().And.ContainSingle();
        }
        
        [Fact]
        public async Task ReadConfiguration_ShouldReturnSingleConfig()
        {
            var host = TestHostFactory.CreateHost(services =>
            {
                services.RemoveService<IRootConfigurationProvider>().AddSingleton<IRootConfigurationProvider, TestRootConfigProvider>();
                services.RemoveService<IClientConfigurationsProvider>().AddSingleton<IClientConfigurationsProvider, TestClientConfigsProvider>();
                services.Configure<TestConfigProviderOptions>(x =>
                {
                    x.RootConfigFilePath = TestEnvironment.GetAssetPath("default-root.config");
                    x.ClientConfigsFolderPath = TestEnvironment.GetAssetPath(TestAssetLocation.ClientsDirectory);
                });
            });

            var conf = host.Services.GetRequiredService<IServiceConfiguration>();
        }
        
    }
}