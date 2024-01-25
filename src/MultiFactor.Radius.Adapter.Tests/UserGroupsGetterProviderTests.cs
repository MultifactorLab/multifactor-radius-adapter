using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using MultiFactor.Radius.Adapter.Configuration;
using MultiFactor.Radius.Adapter.Configuration.Core;
using MultiFactor.Radius.Adapter.Services.Ldap.UserGroupsReading;
using MultiFactor.Radius.Adapter.Tests.Fixtures;
using MultiFactor.Radius.Adapter.Tests.Fixtures.ConfigLoading;

namespace MultiFactor.Radius.Adapter.Tests;

public class UserGroupsGetterProviderTests
{
    [Theory]
    [InlineData(AuthenticationSource.ActiveDirectory)]
    [InlineData(AuthenticationSource.None)]
    public void Get_ActiveDirectoryOrNone_ShouldReturnADGetter(AuthenticationSource source)
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

        var prov = host.Services.GetRequiredService<UserGroupsGetterProvider>();
        var getter = prov.GetUserGroupsGetter(source);

        getter.Should().NotBeNull().And.BeOfType<ActiveDirectoryUserGroupsGetter>();
    }
    
    [Fact]
    public void Get_Ldap_ShouldReturnDefaultGetter()
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

        var prov = host.Services.GetRequiredService<UserGroupsGetterProvider>();
        var getter = prov.GetUserGroupsGetter(AuthenticationSource.Ldap);

        getter.Should().NotBeNull().And.BeOfType<DefaultUserGroupsGetter>();
    }
    
    [Fact]
    public void Get_Radius_ShouldReturnDefaultGetter()
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

        var prov = host.Services.GetRequiredService<UserGroupsGetterProvider>();
        var getter = prov.GetUserGroupsGetter(AuthenticationSource.Radius);

        getter.Should().NotBeNull().And.BeOfType<DefaultUserGroupsGetter>();
    }
}
