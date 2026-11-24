using Multifactor.Radius.Adapter.v2.Application.Core.Enum;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configurations.Exceptions;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configurations.Models;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configurations.Reader;
using static Multifactor.Radius.Adapter.v2.Tests.Fixtures.BypassTestData;

namespace Multifactor.Radius.Adapter.v2.Tests.Infrastructure.Configurations;

/// <summary>
/// Reading and validation of the [bypass] policy settings (bypass-policy) in a client configuration.
/// </summary>
public class ClientConfigurationBypassPolicyTests
{
    [Fact]
    public void XmlBindsClientPolicyAndLdapGroups()
    {
        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, $"""
                <configuration><appSettings>
                  <add key="multifactor-nas-identifier" value="test"/>
                  <add key="multifactor-shared-secret" value="test"/>
                  <add key="radius-shared-secret" value="test"/>
                  <add key="first-factor-authentication-source" value="none"/>
                  <add key="bypass-policy" value="AND"/>
                  <add key="bypass-ip-whitelist" value="{IpRules}"/>
                  <add key="ip-white-list" value="203.0.113.0/24"/>
                </appSettings>
                <ldapServers>
                  <ldapServer connection-string="ldap://domain.your" username="test" password="test"
                    bypass-second-factor-when-api-unreachable-groups="{Groups}"/>
                </ldapServers></configuration>
                """);
            var settings = ConfigurationReader.Read(path)!;
            var config = ClientConfiguration.FromConfiguration(settings, true);
            var policy = config.BypassPolicy;
            Assert.NotNull(policy);
            Assert.Equal(BypassPolicyMode.And, policy.Mode);
            Assert.Equal(3, policy.IpWhitelist.Count);
            var ldap = LdapServerConfiguration.FromConfiguration(Assert.Single(settings.LdapServers), config.Name,
                policy.UsesGroups);
            Assert.Equal(2, ldap.BypassSecondFactorWhenApiUnreachableGroups.Count);
            Assert.Single(config.IpWhiteList);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Theory]
    [InlineData("or", BypassPolicyMode.Or)]
    [InlineData("And", BypassPolicyMode.And)]
    [InlineData(" ip_only ", BypassPolicyMode.IpOnly)]
    [InlineData("GROUP_ONLY", BypassPolicyMode.GroupOnly)]
    public void ParsesPolicyCaseInsensitive(string value, BypassPolicyMode expected)
    {
        Assert.Equal(expected, ClientConfiguration.FromConfiguration(Configuration(value), true).BypassPolicy!.Mode);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("XOR")]
    [InlineData("1")]
    public void InvalidPolicyFailsConfiguration(string policy)
    {
        var config = Configuration(policy);
        var error = Assert.Throws<InvalidConfigurationException>(() => ClientConfiguration.FromConfiguration(config, true));
        Assert.Contains("bypass-policy", error.Message);
    }

    [Theory]
    [InlineData("not-an-ip")]
    [InlineData("192.168.0.1; not-an-ip")]
    [InlineData("10.0.0.0/33")]
    [InlineData("10.0.0.0/-1")]
    [InlineData("2001:db8::/129")]
    public void InvalidIpWhitelistFailsConfiguration(string rules)
    {
        var error = Assert.Throws<InvalidConfigurationException>(() =>
            ClientConfiguration.FromConfiguration(Configuration("OR", ipWhitelist: rules), true));
        Assert.Contains("bypass-ip-whitelist", error.Message);
    }

    [Fact]
    public void MissingPolicyIgnoresBypassIpWhitelist()
    {
        var config = Configuration(null, ipWhitelist: "not-an-ip");
        Assert.Null(ClientConfiguration.FromConfiguration(config, true).BypassPolicy);
    }

    [Fact]
    public void BypassGroupsIntersectingDenyGroupsFailConfiguration()
    {
        var ldap = new LdapServerSection
        {
            ConnectionString = "ldap://domain.your",
            Username = "test",
            Password = "test",
            DenyGroups = DomainAdminsGroup,
            BypassSecondFactorWhenApiUnreachableGroups = Groups
        };
        var error = Assert.Throws<InvalidConfigurationException>(() =>
            LdapServerConfiguration.FromConfiguration(ldap, "test", validateBypassGroups: true));
        Assert.Contains("deny-groups", error.Message);
        Assert.Contains("bypass-second-factor-when-api-unreachable-groups", error.Message);
    }

    [Fact]
    public void MalformedBypassGroupsFailConfigurationForPoliciesThatUseGroups()
    {
        var ldap = new LdapServerSection
        {
            ConnectionString = "ldap://domain.your",
            Username = "test",
            Password = "test",
            BypassSecondFactorWhenApiUnreachableGroups = "not-a-dn"
        };
        var error = Assert.Throws<InvalidConfigurationException>(() =>
            LdapServerConfiguration.FromConfiguration(ldap, "test", validateBypassGroups: true));
        Assert.Contains("bypass-second-factor-when-api-unreachable-groups", error.Message);
        Assert.Empty(LdapServerConfiguration.FromConfiguration(ldap, "test", validateBypassGroups: false)
            .BypassSecondFactorWhenApiUnreachableGroups);
    }
}
