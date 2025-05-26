using System.DirectoryServices.Protocols;
using Microsoft.Extensions.Logging.Abstractions;
using Multifactor.Core.Ldap;
using Multifactor.Core.Ldap.Connection;
using Multifactor.Core.Ldap.Connection.LdapConnectionFactory;
using Multifactor.Radius.Adapter.v2.Core.Ldap.Identity;
using Multifactor.Radius.Adapter.v2.Services.LdapForest;
using Multifactor.Radius.Adapter.v2.Services.NetBios;
using Multifactor.Radius.Adapter.v2.Tests.Fixture;

namespace Multifactor.Radius.Adapter.v2.Tests;

public class NetBiosServiceTests
{
    [Fact]
    public void FindDomain_ShouldReturnDomain()
    {
        var factory = LdapConnectionFactory.Create();

        var sensitiveData = GetConfig();
        var options = new LdapConnectionOptions(
            new LdapConnectionString(sensitiveData["ConnectionString"]),
            AuthType.Basic,
            sensitiveData["Admin"],
            sensitiveData["AdminPwd"]);

        var connection = factory.CreateConnection(options);
        var cache = new ForestMetadataCache();

        var service = new NetBiosService(cache, connection, new NullLogger<NetBiosService>());
        var clientKey = "clientKey";
        var identity = new UserIdentity(sensitiveData["TargetUser"]);
        var result = service.GetDomainByIdentityAsync(clientKey, sensitiveData["Server"], identity);
        Assert.Equal(sensitiveData["Result"], result.StringRepresentation.ToLower());
    }

    [Fact]
    public void FindDomain_Subdomain_ShouldReturnSubdomain()
    {
        var factory = LdapConnectionFactory.Create();

        var sensitiveData = GetConfig();
        var options = new LdapConnectionOptions(
            new LdapConnectionString(sensitiveData["ConnectionString"]),
            AuthType.Basic,
            sensitiveData["Admin"],
            sensitiveData["AdminPwd"]);

        var connection = factory.CreateConnection(options);
        var cache = new ForestMetadataCache();

        var service = new NetBiosService(cache, connection, new NullLogger<NetBiosService>());
        var clientKey = "clientKey";
        var identity = new UserIdentity(sensitiveData["TargetUser2"]);
        var result = service.GetDomainByIdentityAsync(clientKey, sensitiveData["Server2"], identity);
        Assert.Equal(sensitiveData["Result2"], result.StringRepresentation.ToLower());
    }

    private Dictionary<string, string> GetConfig()
    {
        return ConfigUtils.GetConfigSensitiveData("NetBiosServiceTests.txt", "|");
    }
}