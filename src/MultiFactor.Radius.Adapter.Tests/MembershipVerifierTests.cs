using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using MultiFactor.Radius.Adapter.Configuration;
using MultiFactor.Radius.Adapter.Configuration.Core;
using MultiFactor.Radius.Adapter.Services.Ldap;
using MultiFactor.Radius.Adapter.Services.Ldap.MembershipVerification;
using MultiFactor.Radius.Adapter.Tests.Fixtures;
using MultiFactor.Radius.Adapter.Tests.Fixtures.ConfigLoading;
using MultiFactor.Radius.Adapter.Tests.Fixtures.TestData.Profile;

namespace MultiFactor.Radius.Adapter.Tests
{
    public class MembershipVerifierTests
    {
        [Theory]
        [ClassData(typeof(DefaultProfile))]
        public void VerifyMembership_UserIsNotMemberOfSecurityGroup_ShouldReturnBadResult(LdapProfile profile)
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

            var client = ClientConfiguration.CreateBuilder("custom", "shared_secret", AuthenticationSource.None, "key", "secret")
                .SetActiveDirectoryDomain("domain.local")
                .AddActiveDirectoryGroup("Security Group")
                .Build();

            var verifier = host.Services.GetRequiredService<MembershipVerifier>();
            var result = verifier.VerifyMembership(client, profile, client.SplittedActiveDirectoryDomains[0], profile.ExtractUpnBasedUser());

            result.IsSuccess.Should().BeFalse();
        }
        
        [Theory]
        [ClassData(typeof(DefaultProfile))]
        public void VerifyMembership_UserIsMemberOfSecurityGroup_ShouldReturnGoodResult(LdapProfile profile)
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

            var client = ClientConfiguration.CreateBuilder("custom", "shared_secret", AuthenticationSource.None, "key", "secret")
                .SetActiveDirectoryDomain("domain.local")
                .AddActiveDirectoryGroup("Security Group")
                .Build();
            profile.MemberOf.Add("Security Group");

            var verifier = host.Services.GetRequiredService<MembershipVerifier>();
            var result = verifier.VerifyMembership(client, profile, client.SplittedActiveDirectoryDomains[0], profile.ExtractUpnBasedUser());

            result.IsSuccess.Should().BeTrue();
        }
        
        [Theory]
        [ClassData(typeof(DefaultProfile))]
        public void VerifyMembership_2FaGroupSpecified_ShouldReturnTrue(LdapProfile profile)
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

            var client = ClientConfiguration.CreateBuilder("custom", "shared_secret", AuthenticationSource.None, "key", "secret")
                .SetActiveDirectoryDomain("domain.local")
                .AddActiveDirectory2FaGroup("2FA Group")
                .Build();

            var verifier = host.Services.GetRequiredService<MembershipVerifier>();
            var result = verifier.VerifyMembership(client, profile, client.SplittedActiveDirectoryDomains[0], profile.ExtractUpnBasedUser());

            result.IsSuccess.Should().BeTrue();
            result.Are2FaGroupsSpecified.Should().BeTrue();
        }
        
        [Theory]
        [ClassData(typeof(DefaultProfile))]
        public void VerifyMembership_2FaGroupNotSpecified_ShouldReturnFalse(LdapProfile profile)
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

            var client = ClientConfiguration.CreateBuilder("custom", "shared_secret", AuthenticationSource.None, "key", "secret")
                .SetActiveDirectoryDomain("domain.local")
                .Build();

            var verifier = host.Services.GetRequiredService<MembershipVerifier>();
            var result = verifier.VerifyMembership(client, profile, client.SplittedActiveDirectoryDomains[0], profile.ExtractUpnBasedUser());

            result.IsSuccess.Should().BeTrue();
            result.Are2FaGroupsSpecified.Should().BeFalse();
        }
        
        [Theory]
        [ClassData(typeof(DefaultProfile))]
        public void VerifyMembership_UserIsNotMemberOf2FaGroup_ShouldReturnFalse(LdapProfile profile)
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

            var client = ClientConfiguration.CreateBuilder("custom", "shared_secret", AuthenticationSource.None, "key", "secret")
                .SetActiveDirectoryDomain("domain.local")
                .AddActiveDirectory2FaGroup("2FA Group")
                .Build();

            var verifier = host.Services.GetRequiredService<MembershipVerifier>();
            var result = verifier.VerifyMembership(client, profile, client.SplittedActiveDirectoryDomains[0], profile.ExtractUpnBasedUser());

            result.IsSuccess.Should().BeTrue();
            result.IsMemberOf2FaGroups.Should().BeFalse();
        }
        
        [Theory]
        [ClassData(typeof(DefaultProfile))]
        public void VerifyMembership_UserIsMemberOf2FaGroup_ShouldReturnTrue(LdapProfile profile)
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

            var client = ClientConfiguration.CreateBuilder("custom", "shared_secret", AuthenticationSource.None, "key", "secret")
                .SetActiveDirectoryDomain("domain.local")
                .AddActiveDirectory2FaGroup("2FA Group")
                .Build();
            profile.MemberOf.Add("2FA Group");

            var verifier = host.Services.GetRequiredService<MembershipVerifier>();
            var result = verifier.VerifyMembership(client, profile, client.SplittedActiveDirectoryDomains[0], profile.ExtractUpnBasedUser());

            result.IsSuccess.Should().BeTrue();
            result.IsMemberOf2FaGroups.Should().BeTrue();
        }

        [Theory]
        [ClassData(typeof(DefaultProfile))]
        public void VerifyMembership_2FaBypassGroupSpecified_ShouldReturnTrue(LdapProfile profile)
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

            var client = ClientConfiguration.CreateBuilder("custom", "shared_secret", AuthenticationSource.None, "key", "secret")
                .SetActiveDirectoryDomain("domain.local")
                .AddActiveDirectory2FaBypassGroup("2FA Bypass Group")
                .Build();

            var verifier = host.Services.GetRequiredService<MembershipVerifier>();
            var result = verifier.VerifyMembership(client, profile, client.SplittedActiveDirectoryDomains[0], profile.ExtractUpnBasedUser());

            result.IsSuccess.Should().BeTrue();
            result.Are2FaBypassGroupsSpecified.Should().BeTrue();
        }
        
        [Theory]
        [ClassData(typeof(DefaultProfile))]
        public void VerifyMembership_2FaBypassGroupNotSpecified_ShouldReturnFalse(LdapProfile profile)
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

            var client = ClientConfiguration.CreateBuilder("custom", "shared_secret", AuthenticationSource.None, "key", "secret")
                .SetActiveDirectoryDomain("domain.local")
                .Build();

            var verifier = host.Services.GetRequiredService<MembershipVerifier>();
            var result = verifier.VerifyMembership(client, profile, client.SplittedActiveDirectoryDomains[0], profile.ExtractUpnBasedUser());

            result.IsSuccess.Should().BeTrue();
            result.Are2FaBypassGroupsSpecified.Should().BeFalse();
        }

        [Theory]
        [ClassData(typeof(DefaultProfile))]
        public void VerifyMembership_UserIsNotMemberOf2FaBypassGroup_ShouldReturnFalse(LdapProfile profile)
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

            var client = ClientConfiguration.CreateBuilder("custom", "shared_secret", AuthenticationSource.None, "key", "secret")
                .SetActiveDirectoryDomain("domain.local")
                .AddActiveDirectory2FaBypassGroup("2FA Bypass Group")
                .Build();

            var verifier = host.Services.GetRequiredService<MembershipVerifier>();
            var result = verifier.VerifyMembership(client, profile, client.SplittedActiveDirectoryDomains[0], profile.ExtractUpnBasedUser());

            result.IsSuccess.Should().BeTrue();
            result.IsMemberOf2FaBypassGroup.Should().BeFalse();
        }
        
        [Theory]
        [ClassData(typeof(DefaultProfile))]
        public void VerifyMembership_UserIsMemberOf2FaBypassGroup_ShouldReturnTrue(LdapProfile profile)
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

            var client = ClientConfiguration.CreateBuilder("custom", "shared_secret", AuthenticationSource.None, "key", "secret")
                .SetActiveDirectoryDomain("domain.local")
                .AddActiveDirectory2FaBypassGroup("2FA Bypass Group")
                .Build();
            profile.MemberOf.Add("2FA Bypass Group");

            var verifier = host.Services.GetRequiredService<MembershipVerifier>();
            var result = verifier.VerifyMembership(client, profile, client.SplittedActiveDirectoryDomains[0], profile.ExtractUpnBasedUser());

            result.IsSuccess.Should().BeTrue();
            result.IsMemberOf2FaBypassGroup.Should().BeTrue();
        }

        [Theory]
        [ClassData(typeof(DefaultProfile))]
        public void VerifyMembership_UserIsMemberOfGroups_ShouldReturnTrue(LdapProfile profile)
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

            var client = ClientConfiguration.CreateBuilder("custom", "shared_secret", AuthenticationSource.None, "key", "secret")
                .SetActiveDirectoryDomain("domain.local")
                .AddActiveDirectoryGroup("Security Group")
                .AddActiveDirectory2FaGroup("2FA Group")
                .AddActiveDirectory2FaBypassGroup("2FA Bypass Group")
                .Build();
            profile.MemberOf.Add("2FA Bypass Group");
            profile.MemberOf.Add("2FA Group");
            profile.MemberOf.Add("Security Group");

            var verifier = host.Services.GetRequiredService<MembershipVerifier>();
            var result = verifier.VerifyMembership(client, profile, client.SplittedActiveDirectoryDomains[0], profile.ExtractUpnBasedUser());

            result.IsSuccess.Should().BeTrue();
            result.IsMemberOf2FaGroups.Should().BeTrue();
            result.IsMemberOf2FaBypassGroup.Should().BeTrue();
        }
    }
}
