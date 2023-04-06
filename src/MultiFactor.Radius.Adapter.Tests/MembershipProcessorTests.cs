using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using MultiFactor.Radius.Adapter.Configuration;
using MultiFactor.Radius.Adapter.Configuration.Core;
using MultiFactor.Radius.Adapter.Services.Ldap;
using MultiFactor.Radius.Adapter.Services.Ldap.MembershipVerification;
using MultiFactor.Radius.Adapter.Tests.Fixtures;
using MultiFactor.Radius.Adapter.Tests.Fixtures.ConfigLoading;
using MultiFactor.Radius.Adapter.Tests.Fixtures.TestData.Profile;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultiFactor.Radius.Adapter.Tests
{
    public class MembershipProcessorTests
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

            var user = LdapIdentity.ParseUser(profile.Upn);
            var verifier = host.Services.GetRequiredService<MembershipVerifier>();

            var result = verifier.VerifyMembership(client, profile, client.SplittedActiveDirectoryDomains[0], LdapIdentity.ParseUser("user.name@domain.local"));

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

            var user = LdapIdentity.ParseUser(profile.Upn);
            profile.MemberOf.Add("Security Group");
            var verifier = host.Services.GetRequiredService<MembershipVerifier>();

            var result = verifier.VerifyMembership(client, profile, client.SplittedActiveDirectoryDomains[0], LdapIdentity.ParseUser("user.name@domain.local"));

            result.IsSuccess.Should().BeTrue();
        }
    }
}
