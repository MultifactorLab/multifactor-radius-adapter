using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using MultiFactor.Radius.Adapter.Configuration;
using MultiFactor.Radius.Adapter.Configuration.Core;
using MultiFactor.Radius.Adapter.Core.Ldap;
using MultiFactor.Radius.Adapter.Services.Ldap;
using MultiFactor.Radius.Adapter.Services.Ldap.MembershipVerification;
using MultiFactor.Radius.Adapter.Services.Ldap.ProfileLoading;
using MultiFactor.Radius.Adapter.Tests.Fixtures;
using MultiFactor.Radius.Adapter.Tests.Fixtures.ConfigLoading;

namespace MultiFactor.Radius.Adapter.Tests
{
    public class MembershipVerifierTests
    {
        [Fact]
        public void VerifyMembership_UserIsNotMemberOfSecurityGroup_ShouldReturnBadResult()
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

            var profile = GetProfile();
            var client = new ClientConfiguration("custom", "shared_secret", AuthenticationSource.None, "key", "secret")
                .SetActiveDirectoryDomain("domain.local")
                .AddActiveDirectoryGroup("Security Group");

            var verifier = host.Services.GetRequiredService<MembershipVerifier>();
            var result = verifier.VerifyMembership(client, profile, client.SplittedActiveDirectoryDomains[0], profile.ExtractUpnBasedUser());

            result.IsSuccess.Should().BeFalse();
        }
        
        [Fact]
        public void VerifyMembership_UserIsMemberOfSecurityGroup_ShouldReturnGoodResult()
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

            var client = new ClientConfiguration("custom", "shared_secret", AuthenticationSource.None, "key", "secret")
                .SetActiveDirectoryDomain("domain.local")
                .AddActiveDirectoryGroup("Security Group");
            var profile = LdapProfile.CreateBuilder(LdapIdentity.BaseDn("CN=User Name,CN=Users,DC=domain,DC=local"), "CN=User Name,CN=Users,DC=domain,DC=local")
                .SetDisplayName("User Name")
                .SetEmail("username@post.org")
                .SetUpn("user.name@domain.local")
                .AddLdapAttr("sAMAccountName", "user.name")
                .AddLdapAttr("userPrincipalName", "user.name@domain.local")
                .AddMemberOf("Users")
                .AddMemberOf("Security Group")
                .Build();

            var verifier = host.Services.GetRequiredService<MembershipVerifier>();
            var result = verifier.VerifyMembership(client, profile, client.SplittedActiveDirectoryDomains[0], profile.ExtractUpnBasedUser());

            result.IsSuccess.Should().BeTrue();
        }
        
        [Fact]
        public void VerifyMembership_2FaGroupSpecified_ShouldReturnTrue()
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

            var client = new ClientConfiguration("custom", "shared_secret", AuthenticationSource.None, "key", "secret")
                .SetActiveDirectoryDomain("domain.local")
                .AddActiveDirectory2FaGroup("2FA Group");
            var profile = GetProfile();

            var verifier = host.Services.GetRequiredService<MembershipVerifier>();
            var result = verifier.VerifyMembership(client, profile, client.SplittedActiveDirectoryDomains[0], profile.ExtractUpnBasedUser());

            result.IsSuccess.Should().BeTrue();
            result.Are2FaGroupsSpecified.Should().BeTrue();
        }
        
        [Fact]
        public void VerifyMembership_2FaGroupNotSpecified_ShouldReturnFalse()
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

            var client = new ClientConfiguration("custom", "shared_secret", AuthenticationSource.None, "key", "secret")
                .SetActiveDirectoryDomain("domain.local");
            var profile = GetProfile();

            var verifier = host.Services.GetRequiredService<MembershipVerifier>();
            var result = verifier.VerifyMembership(client, profile, client.SplittedActiveDirectoryDomains[0], profile.ExtractUpnBasedUser());

            result.IsSuccess.Should().BeTrue();
            result.Are2FaGroupsSpecified.Should().BeFalse();
        }
        
        [Fact]
        public void VerifyMembership_UserIsNotMemberOf2FaGroup_ShouldReturnFalse()
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

            var client = new ClientConfiguration("custom", "shared_secret", AuthenticationSource.None, "key", "secret")
                .SetActiveDirectoryDomain("domain.local")
                .AddActiveDirectory2FaGroup("2FA Group");
            var profile = GetProfile();

            var verifier = host.Services.GetRequiredService<MembershipVerifier>();
            var result = verifier.VerifyMembership(client, GetProfile(), client.SplittedActiveDirectoryDomains[0], profile.ExtractUpnBasedUser());

            result.IsSuccess.Should().BeTrue();
            result.IsMemberOf2FaGroups.Should().BeFalse();
        }
        
        [Fact]
        public void VerifyMembership_UserIsMemberOf2FaGroup_ShouldReturnTrue()
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

            var client = new ClientConfiguration("custom", "shared_secret", AuthenticationSource.None, "key", "secret")
                .SetActiveDirectoryDomain("domain.local")
                .AddActiveDirectory2FaGroup("2FA Group");
            var profile = LdapProfile.CreateBuilder(LdapIdentity.BaseDn("CN=User Name,CN=Users,DC=domain,DC=local"), "CN=User Name,CN=Users,DC=domain,DC=local")
                .SetDisplayName("User Name")
                .SetEmail("username@post.org")
                .SetUpn("user.name@domain.local")
                .AddLdapAttr("sAMAccountName", "user.name")
                .AddLdapAttr("userPrincipalName", "user.name@domain.local")
                .AddMemberOf("Users")
                .AddMemberOf("2FA Group")
                .Build();

            var verifier = host.Services.GetRequiredService<MembershipVerifier>();
            var result = verifier.VerifyMembership(client, profile, client.SplittedActiveDirectoryDomains[0], profile.ExtractUpnBasedUser());

            result.IsSuccess.Should().BeTrue();
            result.IsMemberOf2FaGroups.Should().BeTrue();
        }

        [Fact]
        public void VerifyMembership_2FaBypassGroupSpecified_ShouldReturnTrue()
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

            var client = new ClientConfiguration("custom", "shared_secret", AuthenticationSource.None, "key", "secret")
                .SetActiveDirectoryDomain("domain.local")
                .AddActiveDirectory2FaBypassGroup("2FA Bypass Group");
            var profile = GetProfile();

            var verifier = host.Services.GetRequiredService<MembershipVerifier>();
            var result = verifier.VerifyMembership(client, profile, client.SplittedActiveDirectoryDomains[0], profile.ExtractUpnBasedUser());

            result.IsSuccess.Should().BeTrue();
            result.Are2FaBypassGroupsSpecified.Should().BeTrue();
        }
        
        [Fact]
        public void VerifyMembership_2FaBypassGroupNotSpecified_ShouldReturnFalse()
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

            var client = new ClientConfiguration("custom", "shared_secret", AuthenticationSource.None, "key", "secret")
                .SetActiveDirectoryDomain("domain.local");
            var profile = GetProfile();

            var verifier = host.Services.GetRequiredService<MembershipVerifier>();
            var result = verifier.VerifyMembership(client, GetProfile(), client.SplittedActiveDirectoryDomains[0], profile.ExtractUpnBasedUser());

            result.IsSuccess.Should().BeTrue();
            result.Are2FaBypassGroupsSpecified.Should().BeFalse();
        }

        [Fact]
        public void VerifyMembership_UserIsNotMemberOf2FaBypassGroup_ShouldReturnFalse()
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

            var client = new ClientConfiguration("custom", "shared_secret", AuthenticationSource.None, "key", "secret")
                .SetActiveDirectoryDomain("domain.local")
                .AddActiveDirectory2FaBypassGroup("2FA Bypass Group");
            var profile = GetProfile();

            var verifier = host.Services.GetRequiredService<MembershipVerifier>();
            var result = verifier.VerifyMembership(client, GetProfile(), client.SplittedActiveDirectoryDomains[0], profile.ExtractUpnBasedUser());

            result.IsSuccess.Should().BeTrue();
            result.IsMemberOf2FaBypassGroup.Should().BeFalse();
        }
        
        [Fact]
        public void VerifyMembership_UserIsMemberOf2FaBypassGroup_ShouldReturnTrue()
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

            var client = new ClientConfiguration("custom", "shared_secret", AuthenticationSource.None, "key", "secret")
                .SetActiveDirectoryDomain("domain.local")
                .AddActiveDirectory2FaBypassGroup("2FA Bypass Group");
            var profile = LdapProfile.CreateBuilder(LdapIdentity.BaseDn("CN=User Name,CN=Users,DC=domain,DC=local"), "CN=User Name,CN=Users,DC=domain,DC=local")
                .SetDisplayName("User Name")
                .SetEmail("username@post.org")
                .SetUpn("user.name@domain.local")
                .AddLdapAttr("sAMAccountName", "user.name")
                .AddLdapAttr("userPrincipalName", "user.name@domain.local")
                .AddMemberOf("Users")
                .AddMemberOf("2FA Bypass Group")
                .Build();

            var verifier = host.Services.GetRequiredService<MembershipVerifier>();
            var result = verifier.VerifyMembership(client, profile, client.SplittedActiveDirectoryDomains[0], profile.ExtractUpnBasedUser());

            result.IsSuccess.Should().BeTrue();
            result.IsMemberOf2FaBypassGroup.Should().BeTrue();
        }

        [Fact]
        public void VerifyMembership_UserIsMemberOfGroups_ShouldReturnTrue()
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

            var client = new ClientConfiguration("custom", "shared_secret", AuthenticationSource.None, "key", "secret")
                .SetActiveDirectoryDomain("domain.local")
                .AddActiveDirectoryGroup("Security Group")
                .AddActiveDirectory2FaGroup("2FA Group")
                .AddActiveDirectory2FaBypassGroup("2FA Bypass Group");
            var profile = LdapProfile.CreateBuilder(LdapIdentity.BaseDn("CN=User Name,CN=Users,DC=domain,DC=local"), "CN=User Name,CN=Users,DC=domain,DC=local")
                .SetDisplayName("User Name")
                .SetEmail("username@post.org")
                .SetUpn("user.name@domain.local")
                .AddLdapAttr("sAMAccountName", "user.name")
                .AddLdapAttr("userPrincipalName", "user.name@domain.local")
                .AddMemberOf("Users")
                .AddMemberOf("2FA Bypass Group")
                .AddMemberOf("2FA Group")
                .AddMemberOf("Security Group")
                .Build();

            var verifier = host.Services.GetRequiredService<MembershipVerifier>();
            var result = verifier.VerifyMembership(client, profile, client.SplittedActiveDirectoryDomains[0], profile.ExtractUpnBasedUser());

            result.IsSuccess.Should().BeTrue();
            result.IsMemberOf2FaGroups.Should().BeTrue();
            result.IsMemberOf2FaBypassGroup.Should().BeTrue();
        }

        private static ILdapProfile GetProfile()
        {
            return LdapProfile.CreateBuilder(LdapIdentity.BaseDn("CN=User Name,CN=Users,DC=domain,DC=local"), "CN=User Name,CN=Users,DC=domain,DC=local")
                .SetDisplayName("User Name")
                .SetEmail("username@post.org")
                .SetUpn("user.name@domain.local")
                .AddLdapAttr("sAMAccountName", "user.name")
                .AddLdapAttr("userPrincipalName", "user.name@domain.local")
                .AddMemberOf("Users")
                .Build();
        }
    }
}
