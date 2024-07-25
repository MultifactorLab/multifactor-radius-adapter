using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration.RootLevel;
using MultiFactor.Radius.Adapter.Services.Ldap;
using MultiFactor.Radius.Adapter.Tests.Fixtures;
using MultiFactor.Radius.Adapter.Tests.Fixtures.ConfigLoading;

namespace MultiFactor.Radius.Adapter.Tests
{
    public class BindIdentityFormatterTests
    {
        [Theory]
        [InlineData("client-ldap-bind-dn-with-ldap.config", "testuser", "uid=testuser,dc=some,dc=dn")]
        [InlineData("client-format-dn-with-ad.config", "testuser", "testuser@base.dn")]
        [InlineData("client-format-dn-with-ad.config", "testuser@upn", "testuser@upn")]
        [InlineData("client-format-dn-with-ad.config", "UPN\\testuser", "testuser@base.dn")]
        public void BindIdentityFormatter_ShouldFormat(string clientConfigPath, string identity, string expectation)
        {
            var host = TestHostFactory.CreateHost(builder =>
            {
                builder.Services.Configure<TestConfigProviderOptions>(x =>
                {
                    x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-multi.config");
                    x.ClientConfigFilePaths = new[]
                    {
                    TestEnvironment.GetAssetPath(TestAssetLocation.ClientsDirectory, clientConfigPath)
                };
                });
            });

            var conf = host.Service<IServiceConfiguration>();
            var clientConfig = conf.Clients.First();
            var user = LdapIdentity.ParseUser(identity);
            var formatter = new BindIdentityFormatter(clientConfig);
            var baseDn = "http://dc.localhost.test/dc=base,dc=dn";
            var newIdentity = formatter.FormatIdentity(user, baseDn);
            newIdentity.Should().NotBeNull().And.BeEquivalentTo(expectation);
        }
    }
}
