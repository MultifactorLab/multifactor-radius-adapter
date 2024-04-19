using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using MultiFactor.Radius.Adapter.Configuration;
using MultiFactor.Radius.Adapter.Services.Ldap;
using MultiFactor.Radius.Adapter.Services.Ldap.MembershipVerification;
using MultiFactor.Radius.Adapter.Services.Ldap.Profile;
using MultiFactor.Radius.Adapter.Tests.Fixtures;
using MultiFactor.Radius.Adapter.Tests.Fixtures.ConfigLoading;

namespace MultiFactor.Radius.Adapter.Tests
{
    public class MembershipVerifierTests
    {
        [Fact]
        public void VerifyMembership_UserIsNotMemberOfSecurityGroup_ShouldReturnBadResult()
        {
            var host = TestHostFactory.CreateHost(builder =>
            {
                builder.Services.Configure<TestConfigProviderOptions>(x =>
                {
                    x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-single.config");
                });
            });

            var profile = GetProfile();
            var client = new ClientConfiguration("custom", "shared_secret", AuthenticationSource.None, "key", "secret")
                .SetActiveDirectoryDomain("domain.local")
                .AddActiveDirectoryGroup("Security Group");

            var verifier = host.Service<MembershipVerifier>();
            var result = verifier.VerifyMembership(client, profile, client.SplittedActiveDirectoryDomains[0], profile.ExtractUpnBasedUser());

            result.IsSuccess.Should().BeFalse();
        }
        
        [Fact]
        public void VerifyMembership_UserIsMemberOfSecurityGroup_ShouldReturnGoodResult()
        {
            var host = TestHostFactory.CreateHost(builder =>
            {
                builder.Services.Configure<TestConfigProviderOptions>(x =>
                {
                    x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-single.config");
                });
            });

            var client = new ClientConfiguration("custom", "shared_secret", AuthenticationSource.None, "key", "secret")
                .SetActiveDirectoryDomain("domain.local")
                .AddActiveDirectoryGroup("Security Group");
            var attrs = new LdapAttributes("CN=User Name,CN=Users,DC=domain,DC=local")
                .Add("displayName", "User Name")
                .Add("mail", "username@post.org")
                .Add("userPrincipalName", "user.name@domain.local")
                .Add("sAMAccountName", "user.name")
                .Add("memberOf", new[] { "Users", "Security Group" });
            var profile = new LdapProfile(LdapIdentity.BaseDn("CN=User Name,CN=Users,DC=domain,DC=local"),
                attrs,
                Array.Empty<string>(),
                null);

            var verifier = host.Service<MembershipVerifier>();
            var result = verifier.VerifyMembership(client, profile, client.SplittedActiveDirectoryDomains[0], profile.ExtractUpnBasedUser());

            result.IsSuccess.Should().BeTrue();
        }
        
        [Fact]
        public void VerifyMembership_2FaGroupSpecified_ShouldReturnTrue()
        {
            var host = TestHostFactory.CreateHost(builder =>
            {
                builder.Services.Configure<TestConfigProviderOptions>(x =>
                {
                    x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-single.config");
                });
            });

            var client = new ClientConfiguration("custom", "shared_secret", AuthenticationSource.None, "key", "secret")
                .SetActiveDirectoryDomain("domain.local")
                .AddActiveDirectory2FaGroup("2FA Group");
            var profile = GetProfile();

            var verifier = host.Service<MembershipVerifier>();
            var result = verifier.VerifyMembership(client, profile, client.SplittedActiveDirectoryDomains[0], profile.ExtractUpnBasedUser());

            result.IsSuccess.Should().BeTrue();
            result.Are2FaGroupsSpecified.Should().BeTrue();
        }
        
        [Fact]
        public void VerifyMembership_2FaGroupNotSpecified_ShouldReturnFalse()
        {
            var host = TestHostFactory.CreateHost(builder =>
            {
                builder.Services.Configure<TestConfigProviderOptions>(x =>
                {
                    x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-single.config");
                });
            });

            var client = new ClientConfiguration("custom", "shared_secret", AuthenticationSource.None, "key", "secret")
                .SetActiveDirectoryDomain("domain.local");
            var profile = GetProfile();

            var verifier = host.Service<MembershipVerifier>();
            var result = verifier.VerifyMembership(client, profile, client.SplittedActiveDirectoryDomains[0], profile.ExtractUpnBasedUser());

            result.IsSuccess.Should().BeTrue();
            result.Are2FaGroupsSpecified.Should().BeFalse();
        }
        
        [Fact]
        public void VerifyMembership_UserIsNotMemberOf2FaGroup_ShouldReturnFalse()
        {
            var host = TestHostFactory.CreateHost(builder =>
            {
                builder.Services.Configure<TestConfigProviderOptions>(x =>
                {
                    x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-single.config");
                });
            });

            var client = new ClientConfiguration("custom", "shared_secret", AuthenticationSource.None, "key", "secret")
                .SetActiveDirectoryDomain("domain.local")
                .AddActiveDirectory2FaGroup("2FA Group");
            var profile = GetProfile();

            var verifier = host.Service<MembershipVerifier>();
            var result = verifier.VerifyMembership(client, GetProfile(), client.SplittedActiveDirectoryDomains[0], profile.ExtractUpnBasedUser());

            result.IsSuccess.Should().BeTrue();
            result.IsMemberOf2FaGroups.Should().BeFalse();
        }
        
        [Fact]
        public void VerifyMembership_UserIsMemberOf2FaGroup_ShouldReturnTrue()
        {
            var host = TestHostFactory.CreateHost(builder =>
            {
                builder.Services.Configure<TestConfigProviderOptions>(x =>
                {
                    x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-single.config");
                });
            });

            var client = new ClientConfiguration("custom", "shared_secret", AuthenticationSource.None, "key", "secret")
                .SetActiveDirectoryDomain("domain.local")
                .AddActiveDirectory2FaGroup("2FA Group");
            var attrs = new LdapAttributes("CN=User Name,CN=Users,DC=domain,DC=local");
            attrs.Add("displayName", "User Name")
                .Add("mail", "username@post.org")
                .Add("userPrincipalName", "user.name@domain.local")
                .Add("sAMAccountName", "user.name")
                .Add("memberOf", new[] { "Users", "2FA Group" });
            var profile = new LdapProfile(LdapIdentity.BaseDn("CN=User Name,CN=Users,DC=domain,DC=local"), attrs, Array.Empty<string>(), null);

            var verifier = host.Service<MembershipVerifier>();
            var result = verifier.VerifyMembership(client, profile, client.SplittedActiveDirectoryDomains[0], profile.ExtractUpnBasedUser());

            result.IsSuccess.Should().BeTrue();
            result.IsMemberOf2FaGroups.Should().BeTrue();
        }

        [Fact]
        public void VerifyMembership_2FaBypassGroupSpecified_ShouldReturnTrue()
        {
            var host = TestHostFactory.CreateHost(builder =>
            {
                builder.Services.Configure<TestConfigProviderOptions>(x =>
                {
                    x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-single.config");
                });
            });

            var client = new ClientConfiguration("custom", "shared_secret", AuthenticationSource.None, "key", "secret")
                .SetActiveDirectoryDomain("domain.local")
                .AddActiveDirectory2FaBypassGroup("2FA Bypass Group");
            var profile = GetProfile();

            var verifier = host.Service<MembershipVerifier>();
            var result = verifier.VerifyMembership(client, profile, client.SplittedActiveDirectoryDomains[0], profile.ExtractUpnBasedUser());

            result.IsSuccess.Should().BeTrue();
            result.Are2FaBypassGroupsSpecified.Should().BeTrue();
        }
        
        [Fact]
        public void VerifyMembership_2FaBypassGroupNotSpecified_ShouldReturnFalse()
        {
            var host = TestHostFactory.CreateHost(builder =>
            {
                builder.Services.Configure<TestConfigProviderOptions>(x =>
                {
                    x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-single.config");
                });
            });

            var client = new ClientConfiguration("custom", "shared_secret", AuthenticationSource.None, "key", "secret")
                .SetActiveDirectoryDomain("domain.local");
            var profile = GetProfile();

            var verifier = host.Service<MembershipVerifier>();
            var result = verifier.VerifyMembership(client, GetProfile(), client.SplittedActiveDirectoryDomains[0], profile.ExtractUpnBasedUser());

            result.IsSuccess.Should().BeTrue();
            result.Are2FaBypassGroupsSpecified.Should().BeFalse();
        }

        [Fact]
        public void VerifyMembership_UserIsNotMemberOf2FaBypassGroup_ShouldReturnFalse()
        {
            var host = TestHostFactory.CreateHost(builder =>
            {
                builder.Services.Configure<TestConfigProviderOptions>(x =>
                {
                    x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-single.config");
                });
            });

            var client = new ClientConfiguration("custom", "shared_secret", AuthenticationSource.None, "key", "secret")
                .SetActiveDirectoryDomain("domain.local")
                .AddActiveDirectory2FaBypassGroup("2FA Bypass Group");
            var profile = GetProfile();

            var verifier = host.Service<MembershipVerifier>();
            var result = verifier.VerifyMembership(client, GetProfile(), client.SplittedActiveDirectoryDomains[0], profile.ExtractUpnBasedUser());

            result.IsSuccess.Should().BeTrue();
            result.IsMemberOf2FaBypassGroup.Should().BeFalse();
        }
        
        [Fact]
        public void VerifyMembership_UserIsMemberOf2FaBypassGroup_ShouldReturnTrue()
        {
            var host = TestHostFactory.CreateHost(builder =>
            {
                builder.Services.Configure<TestConfigProviderOptions>(x =>
                {
                    x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-single.config");
                });
            });

            var client = new ClientConfiguration("custom", "shared_secret", AuthenticationSource.None, "key", "secret")
                .SetActiveDirectoryDomain("domain.local")
                .AddActiveDirectory2FaBypassGroup("2FA Bypass Group");
            var attrs = new LdapAttributes("CN=User Name,CN=Users,DC=domain,DC=local");
            attrs.Add("displayName", "User Name")
                .Add("mail", "username@post.org")
                .Add("userPrincipalName", "user.name@domain.local")
                .Add("sAMAccountName", "user.name")
                .Add("memberOf", new[] { "Users", "2FA Bypass Group"});
            var profile = new LdapProfile(LdapIdentity.BaseDn("CN=User Name,CN=Users,DC=domain,DC=local"), attrs, Array.Empty<string>(), null);

            var verifier = host.Service<MembershipVerifier>();
            var result = verifier.VerifyMembership(client, profile, client.SplittedActiveDirectoryDomains[0], profile.ExtractUpnBasedUser());

            result.IsSuccess.Should().BeTrue();
            result.IsMemberOf2FaBypassGroup.Should().BeTrue();
        }

        [Fact]
        public void VerifyMembership_UserIsMemberOfGroups_ShouldReturnTrue()
        {
            var host = TestHostFactory.CreateHost(builder =>
            {
                builder.Services.Configure<TestConfigProviderOptions>(x =>
                {
                    x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-single.config");
                });
            });

            var client = new ClientConfiguration("custom", "shared_secret", AuthenticationSource.None, "key", "secret")
                .SetActiveDirectoryDomain("domain.local")
                .AddActiveDirectoryGroup("Security Group")
                .AddActiveDirectory2FaGroup("2FA Group")
                .AddActiveDirectory2FaBypassGroup("2FA Bypass Group");
            var attrs = new LdapAttributes("CN=User Name,CN=Users,DC=domain,DC=local");
            attrs.Add("displayName", "User Name")
                .Add("mail", "username@post.org")
                .Add("userPrincipalName", "user.name@domain.local")
                .Add("sAMAccountName", "user.name")
                .Add("memberOf", new[] { "Users", "2FA Bypass Group", "2FA Group", "Security Group" });
            var profile = new LdapProfile(LdapIdentity.BaseDn("CN=User Name,CN=Users,DC=domain,DC=local"), attrs, Array.Empty<string>(), null);

            var verifier = host.Service<MembershipVerifier>();
            var result = verifier.VerifyMembership(client, profile, client.SplittedActiveDirectoryDomains[0], profile.ExtractUpnBasedUser());

            result.IsSuccess.Should().BeTrue();
            result.IsMemberOf2FaGroups.Should().BeTrue();
            result.IsMemberOf2FaBypassGroup.Should().BeTrue();
        }

        private static LdapProfile GetProfile()
        {
            var attrs = new LdapAttributes("CN=User Name,CN=Users,DC=domain,DC=local")
                .Add("displayName", "User Name")
                .Add("mail", "username@post.org")
                .Add("userPrincipalName", "user.name@domain.local")
                .Add("sAMAccountName", "user.name")
                .Add("memberOf", "Users");
            return new LdapProfile(
                LdapIdentity.BaseDn("CN=User Name,CN=Users,DC=domain,DC=local"), 
                attrs,
                Array.Empty<string>(),
                null);
        }
    }
}
