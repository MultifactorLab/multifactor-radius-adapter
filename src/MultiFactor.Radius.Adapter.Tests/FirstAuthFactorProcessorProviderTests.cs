using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using MultiFactor.Radius.Adapter.Configuration;
using MultiFactor.Radius.Adapter.Configuration.Core;
using MultiFactor.Radius.Adapter.Server.FirstAuthFactorProcessing;
using MultiFactor.Radius.Adapter.Tests.Fixtures;
using MultiFactor.Radius.Adapter.Tests.Fixtures.ConfigLoading;

namespace MultiFactor.Radius.Adapter.Tests
{
    public class FirstAuthFactorProcessorProviderTests
    {
        [Fact]
        public void Get_ShouldReturnDefault()
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

            var prov = host.Services.GetRequiredService<IFirstAuthFactorProcessorProvider>();
            var getter = prov.GetProcessor(AuthenticationSource.None);

            getter.Should().NotBeNull().And.BeOfType<DefaultFirstAuthFactorProcessor>();
        }
        
        [Theory]
        [InlineData(AuthenticationSource.ActiveDirectory)]
        [InlineData(AuthenticationSource.Ldap)]
        public void Get_ShouldReturnLdap(AuthenticationSource source)
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

            var prov = host.Services.GetRequiredService<IFirstAuthFactorProcessorProvider>();
            var getter = prov.GetProcessor(source);

            getter.Should().NotBeNull().And.BeOfType<LdapFirstAuthFactorProcessor>();
        }
        
        [Fact]
        public void Get_ShouldReturnRadius()
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

            var prov = host.Services.GetRequiredService<IFirstAuthFactorProcessorProvider>();
            var getter = prov.GetProcessor(AuthenticationSource.Radius);

            getter.Should().NotBeNull().And.BeOfType<RadiusFirstAuthFactorProcessor>();
        }
    }
}
