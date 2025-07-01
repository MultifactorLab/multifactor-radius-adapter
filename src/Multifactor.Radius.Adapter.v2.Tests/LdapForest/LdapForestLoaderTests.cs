using System.DirectoryServices.Protocols;
using Microsoft.Extensions.Logging.Abstractions;
using Multifactor.Core.Ldap;
using Multifactor.Core.Ldap.Connection;
using Multifactor.Core.Ldap.Connection.LdapConnectionFactory;
using Multifactor.Core.Ldap.Name;
using Multifactor.Radius.Adapter.v2.Services.LdapForest;
using Multifactor.Radius.Adapter.v2.Tests.Fixture;

namespace Multifactor.Radius.Adapter.v2.Tests.LdapForest;

[Collection("ActiveDirectory")]
public class LdapForestLoaderTests
{
    [Fact]
    public void LoadLdapForest_ShouldLoadForestSchema()
    {
        var factory = LdapConnectionFactory.Create();

        var sensitiveData = GetConfig();
        var options = new LdapConnectionOptions(
            new LdapConnectionString(sensitiveData["ConnectionString"]),
            AuthType.Basic,
            sensitiveData["Admin"],
            sensitiveData["AdminPwd"]);

        using var connection = factory.CreateConnection(options);

        var loader = new ForestSchemaLoader(connection, NullLogger.Instance);
        var result = loader.Load(new DistinguishedName(sensitiveData["Dn"]));
        Assert.NotNull(result);
        Assert.NotEmpty(result.DomainNameSuffixes);

        var expected = new string[]
        {
            sensitiveData["suf1"],
            sensitiveData["suf2"],
            sensitiveData["suf3"],
            sensitiveData["suf4"],
            sensitiveData["suf5"],
            sensitiveData["suf6"]
        }.Select(x => x.ToLower()).Order();
        
        var actual = result.DomainNameSuffixes.Select(x => $"{x.Key},{x.Value}".ToLower()).Order();
        Assert.True(expected.SequenceEqual(actual));
    }

    private Dictionary<string, string> GetConfig()
    {
        return ConfigUtils.GetConfigSensitiveData("LdapForestLoader.txt", "|");
    }
}