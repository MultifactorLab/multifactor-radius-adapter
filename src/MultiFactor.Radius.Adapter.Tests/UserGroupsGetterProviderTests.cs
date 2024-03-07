using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MultiFactor.Radius.Adapter.Configuration;
using MultiFactor.Radius.Adapter.Configuration.Core;
using MultiFactor.Radius.Adapter.Services.Ldap.UserGroupsReading;
using MultiFactor.Radius.Adapter.Tests.Fixtures;
using MultiFactor.Radius.Adapter.Tests.Fixtures.ConfigLoading;

namespace MultiFactor.Radius.Adapter.Tests;

[Trait("Category", "Profile")]
public class UserGroupsGetterProviderTests
{
    private IHost CreateTestingHost()
    {
        return TestHostFactory.CreateHost(services =>
        {
            services.RemoveService<IRootConfigurationProvider>().AddSingleton<IRootConfigurationProvider, TestRootConfigProvider>();
            services.RemoveService<IClientConfigurationsProvider>().AddSingleton<IClientConfigurationsProvider, TestClientConfigsProvider>();
            services.Configure<TestConfigProviderOptions>(x =>
            {
                x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-single.config");
            });
        });
    }

    [Theory]
    [InlineData(AuthenticationSource.ActiveDirectory)]
    [InlineData(AuthenticationSource.None)]
    public void Get_ActiveDirectoryOrNone_ShouldReturnADGetter(AuthenticationSource source)
    {
        var host = CreateTestingHost();

        var prov = host.Services.GetRequiredService<UserGroupsGetterProvider>();
        var getter = prov.GetUserGroupsGetter(source);

        getter.Should().NotBeNull().And.BeOfType<ActiveDirectoryUserGroupsGetter>();
    }
    
    [Theory]
    [InlineData(AuthenticationSource.Radius)]
    [InlineData(AuthenticationSource.Ldap)]
    [InlineData((AuthenticationSource)(-1))]
    public void Get_LdapOrRadiusOrUnknown_ShouldReturnDefaultGetter(AuthenticationSource source)
    {
        var host = CreateTestingHost();

        var prov = host.Services.GetRequiredService<UserGroupsGetterProvider>();
        var getter = prov.GetUserGroupsGetter(source);

        getter.Should().NotBeNull().And.BeOfType<DefaultUserGroupsGetter>();
    }
}
