using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MultiFactor.Radius.Adapter.Configuration;
using MultiFactor.Radius.Adapter.Configuration.Core;
using MultiFactor.Radius.Adapter.Services.Ldap.UserGroupsReading;
using MultiFactor.Radius.Adapter.Tests.Fixtures;
using MultiFactor.Radius.Adapter.Tests.Fixtures.ConfigLoading;

namespace MultiFactor.Radius.Adapter.Tests;

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
    [InlineData(AuthenticationSource.ActiveDirectory, LdapCatalogType.ActiveDirectory)]
    [InlineData(AuthenticationSource.Radius, LdapCatalogType.ActiveDirectory)]
    public void Get_ActiveDirectoryOrNone_ShouldReturnADGetter(AuthenticationSource authenticationSource, LdapCatalogType ldapCatalog)
    {
        var host = CreateTestingHost();

        var prov = host.Services.GetRequiredService<UserGroupsGetterProvider>();
        var getter = prov.GetUserGroupsGetter(authenticationSource, ldapCatalog);

        getter.Should().NotBeNull().And.BeOfType<ActiveDirectoryUserGroupsGetter>();
    }
    
    [Theory]
    [InlineData(AuthenticationSource.Radius, LdapCatalogType.OpenLdap)]
    [InlineData(AuthenticationSource.Ldap, LdapCatalogType.ActiveDirectory)]
    [InlineData(AuthenticationSource.ActiveDirectory, LdapCatalogType.FreeIpa)]
    [InlineData((AuthenticationSource)(-1), LdapCatalogType.ActiveDirectory)]
    public void Get_LdapOrRadiusOrUnknown_ShouldReturnDefaultGetter(AuthenticationSource authenticationSource, LdapCatalogType ldapCatalog)
    {
        var host = CreateTestingHost();

        var prov = host.Services.GetRequiredService<UserGroupsGetterProvider>();
        var getter = prov.GetUserGroupsGetter(authenticationSource, ldapCatalog);

        getter.Should().NotBeNull().And.BeOfType<DefaultUserGroupsGetter>();
    }

}
