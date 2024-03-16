using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using MultiFactor.Radius.Adapter.Configuration;
using MultiFactor.Radius.Adapter.Services.Ldap.UserGroupsReading;
using MultiFactor.Radius.Adapter.Tests.Fixtures;
using MultiFactor.Radius.Adapter.Tests.Fixtures.ConfigLoading;

namespace MultiFactor.Radius.Adapter.Tests;

[Trait("Category", "Profile")]
public class UserGroupsGetterProviderTests
{
    [Theory]
    [InlineData(AuthenticationSource.ActiveDirectory)]
    [InlineData(AuthenticationSource.None)]
    public void Get_ActiveDirectoryOrNone_ShouldReturnADGetter(AuthenticationSource source)
    {
        var host = TestHostFactory.CreateHost(builder =>
        {
            builder.Services.Configure<TestConfigProviderOptions>(x =>
            {
                x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-single.config");
            });
        });

        var prov = host.Service<UserGroupsGetterProvider>();
        var getter = prov.GetUserGroupsGetter(source);

        getter.Should().NotBeNull().And.BeOfType<ActiveDirectoryUserGroupsGetter>();
    }
    
    [Theory]
    [InlineData(AuthenticationSource.Radius)]
    [InlineData(AuthenticationSource.Ldap)]
    [InlineData((AuthenticationSource)(-1))]
    public void Get_LdapOrRadiusOrUnknown_ShouldReturnDefaultGetter(AuthenticationSource source)
    {
        var host = TestHostFactory.CreateHost(builder =>
        {
            builder.Services.Configure<TestConfigProviderOptions>(x =>
            {
                x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-single.config");
            });
        });

        var prov = host.Service<UserGroupsGetterProvider>();
        var getter = prov.GetUserGroupsGetter(source);

        getter.Should().NotBeNull().And.BeOfType<DefaultUserGroupsGetter>();
    }
}
