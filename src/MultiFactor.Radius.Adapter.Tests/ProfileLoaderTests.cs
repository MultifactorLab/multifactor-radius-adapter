using FluentAssertions;
using LdapForNet;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using MultiFactor.Radius.Adapter.Configuration;
using MultiFactor.Radius.Adapter.Core.Exceptions;
using MultiFactor.Radius.Adapter.Core.Ldap;
using MultiFactor.Radius.Adapter.Server;
using MultiFactor.Radius.Adapter.Services.Ldap;
using MultiFactor.Radius.Adapter.Services.Ldap.Profile;
using MultiFactor.Radius.Adapter.Tests.Fixtures;
using MultiFactor.Radius.Adapter.Tests.Fixtures.ConfigLoading;
using MultiFactor.Radius.Adapter.Tests.Fixtures.LdapResponse;
using static LdapForNet.Native.Native;
using LdapAttributes = MultiFactor.Radius.Adapter.Services.Ldap.Profile.LdapAttributes;

namespace MultiFactor.Radius.Adapter.Tests;

[Trait("Category", "Ldap")]
[Trait("Category", "Ldap Profile")]
public class ProfileLoaderTests
{
    [Fact]
    public async Task Load_NonExistentUser_ShouldThrow()
    {
        var host = TestHostFactory.CreateHost(builder =>
        {
            builder.Services.Configure<TestConfigProviderOptions>(x =>
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

        var clientConfig = new ClientConfiguration("custom", "shared_secret", AuthenticationSource.ActiveDirectory, "key", "secret")
            .SetLoadActiveDirectoryNestedGroups(false);
        var loader = host.Service<ProfileLoader>();

        var act = async () => await loader.LoadAsync(clientConfig, adapter.Object, LdapIdentity.ParseUser("some.user@domain.local"));

        await act.Should().ThrowAsync<LdapUserNotFoundException>();
    }
    
    [Fact]
    public async Task Load_ExistentUser_ShouldReturnProfile()
    {
        var host = TestHostFactory.CreateHost(builder =>
        {
            builder.Services.Configure<TestConfigProviderOptions>(x =>
            {
                x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-single.config");
            });
        });

        var attrs = new LdapAttributes("CN=User Name,CN=Users,DC=domain,DC=local");
        var baseDn = LdapIdentity.BaseDn("CN=User Name,CN=Users,DC=domain,DC=local");
        var expectedProfile = new LdapProfile(baseDn, attrs, Array.Empty<string>(), null);
        attrs.Add("displayName", "User Name")
            .Add("distinguishedName", "CN=User Name,CN=Users,DC=domain,DC=local")
            .Add("mail", "username@post.org")
            .Add("userPrincipalName", "user.name@domain.local")
            .Add("sAMAccountName", "user.name")
            .Add("memberOf", "Users");

        var entry = LdapEntryFactory.Create("CN=User Name,CN=Users,DC=domain,DC=local", x =>
        {
            x.Add("sAMAccountName", "user.name")
             .Add("displayName", "User Name")
             .Add("distinguishedName", "CN=User Name,CN=Users,DC=domain,DC=local")
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

        var clientConfig = new ClientConfiguration("custom", "shared_secret", AuthenticationSource.ActiveDirectory, "key", "secret")
            .SetLoadActiveDirectoryNestedGroups(false)
            .SetUseAttributeAsIdentity("mail");
        var loader = host.Service<ProfileLoader>();

        var profile = await loader.LoadAsync(clientConfig, adapter.Object, LdapIdentity.ParseUser("some.user@domain.local"));

        profile.Should().NotBeNull();
        profile.BaseDn.Should().Be(expectedProfile.BaseDn);
        profile.DisplayName.Should().Be(expectedProfile.DisplayName);
        profile.DistinguishedNameEscaped.Should().Be(expectedProfile.DistinguishedNameEscaped);
        profile.DisplayName.Should().Be(expectedProfile.DisplayName);
        profile.Email.Should().Be(expectedProfile.Email);
        profile.Phone.Should().Be(expectedProfile.Phone);
        profile.Upn.Should().Be(expectedProfile.Upn);
        profile.MemberOf.Should().BeEquivalentTo(expectedProfile.MemberOf);
        profile.SecondFactorIdentity.Should().BeEquivalentTo(expectedProfile.Email);
    }

    [Fact]
    public async Task Load_HasReplyAttrs_ShouldLoadReplyAttrs()
    {
        var host = TestHostFactory.CreateHost(builder =>
        {
            builder.Services.Configure<TestConfigProviderOptions>(x =>
            {
                x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-single.config");
            });
        });

        var entry = LdapEntryFactory.Create("CN=User Name,CN=Users,DC=domain,DC=local", x =>
        {
            x.Add("myAttrOne", "User Name");
            x.Add("myAttrTwo", "mail@mail.dev");
        });
        var domain = LdapDomain.Parse("dc=domain,dc=local");

        var adapter = new Mock<ILdapConnectionAdapter>();
        adapter.Setup(x => x.WhereAmIAsync()).ReturnsAsync(domain);
        adapter.Setup(x => x.SearchQueryAsync(It.Is<string>(x => x == domain.Name),
            It.IsAny<string>(),
            It.Is<LdapSearchScope>(x => x == LdapSearchScope.LDAP_SCOPE_SUB),
            It.IsAny<string[]>()))
            .ReturnsAsync(new[] { entry });

        var clientConfig = new ClientConfiguration("custom", "shared_secret", AuthenticationSource.ActiveDirectory, "key", "secret")
            .AddRadiusReplyAttribute("radiusAttr", new[] 
            { 
                new RadiusReplyAttributeValue("myAttrOne"),
                new RadiusReplyAttributeValue("myAttrTwo")
            })
            .SetLoadActiveDirectoryNestedGroups(false);
        var loader = host.Service<ProfileLoader>();

        var profile = await loader.LoadAsync(clientConfig, adapter.Object, LdapIdentity.ParseUser("some.user@domain.local"));

        var myAttrOne = profile.Attributes.GetValue("myAttrOne");
        Assert.Equal("User Name", myAttrOne);
        
        var myAttrTwo = profile.Attributes.GetValue("myAttrTwo");
        Assert.Equal("mail@mail.dev", myAttrTwo);
    }
}
