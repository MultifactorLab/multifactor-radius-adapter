using FluentAssertions;
using LdapForNet;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using MultiFactor.Radius.Adapter.Configuration;
using MultiFactor.Radius.Adapter.Configuration.Core;
using MultiFactor.Radius.Adapter.Core.Exceptions;
using MultiFactor.Radius.Adapter.Core.Ldap;
using MultiFactor.Radius.Adapter.Services.Ldap;
using MultiFactor.Radius.Adapter.Services.Ldap.ProfileLoading;
using MultiFactor.Radius.Adapter.Tests.Fixtures;
using MultiFactor.Radius.Adapter.Tests.Fixtures.ConfigLoading;
using MultiFactor.Radius.Adapter.Tests.Fixtures.LdapResponse;
using static LdapForNet.Native.Native;

namespace MultiFactor.Radius.Adapter.Tests;

public class ProfileLoaderTests
{
    [Fact]
    public async Task Load_NonExistentUser_ShouldThrow()
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

        var domain = LdapDomain.Parse("dc=domain,dc=local");

        var adapter = new Mock<ILdapConnectionAdapter>();
        adapter.Setup(x => x.WhereAmIAsync()).ReturnsAsync(domain);
        adapter.Setup(x => x.SearchQueryAsync(It.Is<string>(x => x == domain.Name), 
            It.IsAny<string>(), 
            It.Is<LdapSearchScope>(x => x == LdapSearchScope.LDAP_SCOPE_SUB), 
            It.IsAny<string[]>()))
            .ReturnsAsync(Array.Empty<LdapEntry>());

        var clientConfig = ClientConfiguration.CreateBuilder("custom", "shared_secret", AuthenticationSource.ActiveDirectory, "key", "secret")
            .SetLoadActiveDirectoryNestedGroups(false)
            .Build();
        var loader = host.Services.GetRequiredService<ProfileLoader>();

        var act = async () => await loader.LoadAsync(clientConfig, adapter.Object, LdapIdentity.ParseUser("some.user@domain.local"));

        await act.Should().ThrowAsync<LdapUserNotFoundException>();
    }
    
    [Fact]
    public async Task Load_ExistentUser_ShouldReturnProfile()
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

        var expectedProfile = LdapProfile.CreateBuilder(LdapIdentity.BaseDn("CN=User Name,CN=Users,DC=domain,DC=local"), "CN=User Name,CN=Users,DC=domain,DC=local")
            .SetDisplayName("User Name")
            .SetEmail("username@post.org")
            .SetUpn("user.name@domain.local")
            .AddLdapAttr("sAMAccountName", "user.name")
            .AddLdapAttr("userPrincipalName", "user.name@domain.local")
            .AddMemberOf("Users")
            .Build();

        var entry = LdapEntryFactory.Create("CN=User Name,CN=Users,DC=domain,DC=local", x =>
        {
            x.Add("sAMAccountName", "user.name")
            .Add("displayName", "User Name")
            .Add("memberOf", "CN=Users,DC=domain,DC=local")
            .Add("mail", "username@post.org")
            .Add("userPrincipalName", "user.name@domain.local");
        });
        var domain = LdapDomain.Parse("dc=domain,dc=local");

        var adapter = new Mock<ILdapConnectionAdapter>();
        adapter.Setup(x => x.WhereAmIAsync()).ReturnsAsync(domain);
        adapter.Setup(x => x.SearchQueryAsync(It.Is<string>(x => x == domain.Name), 
            It.IsAny<string>(), 
            It.Is<LdapSearchScope>(x => x == LdapSearchScope.LDAP_SCOPE_SUB), 
            It.IsAny<string[]>()))
            .ReturnsAsync(new[] { entry } );

        var clientConfig = ClientConfiguration.CreateBuilder("custom", "shared_secret", AuthenticationSource.ActiveDirectory, "key", "secret")
            .SetLoadActiveDirectoryNestedGroups(false)
            .Build();
        var loader = host.Services.GetRequiredService<ProfileLoader>();

        var profile = await loader.LoadAsync(clientConfig, adapter.Object, LdapIdentity.ParseUser("some.user@domain.local"));

        profile.Should().NotBeNull();
        profile.BaseDn.Should().Be(expectedProfile.BaseDn);
        profile.DisplayName.Should().Be(expectedProfile.DisplayName);
        profile.DistinguishedNameEscaped.Should().Be(expectedProfile.DistinguishedNameEscaped);
        profile.DisplayName.Should().Be(expectedProfile.DisplayName);
        profile.Email.Should().Be(expectedProfile.Email);
        profile.Phone.Should().Be(expectedProfile.Phone);
        profile.Upn.Should().Be(expectedProfile.Upn);
        profile.LdapAttrs.Should().BeEmpty();
        profile.MemberOf.Should().BeEquivalentTo(expectedProfile.MemberOf);
    }
}
