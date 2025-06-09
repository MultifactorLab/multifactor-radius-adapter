using System.DirectoryServices.Protocols;
using Microsoft.Extensions.Logging.Abstractions;
using Multifactor.Core.Ldap;
using Multifactor.Core.Ldap.Connection;
using Multifactor.Core.Ldap.Connection.LdapConnectionFactory;
using Multifactor.Core.Ldap.Schema;
using Multifactor.Radius.Adapter.v2.Services.Ldap;
using Multifactor.Radius.Adapter.v2.Tests.Fixture;

namespace Multifactor.Radius.Adapter.v2.Tests;

public class CustomLdapSchemaLoaderTests
{
    [Fact]
    public void CustomLdapSchemaLoader_ShouldLoadSchema()
    {
        var config = GetConfig();
        var loader = new LdapSchemaLoader(LdapConnectionFactory.Create());
        var customLdapSchemaLoader = new CustomLdapSchemaLoader(loader, NullLogger<ILdapSchemaLoader>.Instance);
        var connectionOptions = new LdapConnectionOptions(
            new LdapConnectionString(config["ConnectionString"]),
            AuthType.Basic,
            config["UserName"],
            config["Password"]);
        
        var schema = customLdapSchemaLoader.Load(connectionOptions);
        
        Assert.NotNull(schema);
        Assert.Equal(LdapImplementation.ActiveDirectory, schema.LdapServerImplementation);
        Assert.Equal(config["ExpectedDn"], schema.NamingContext.StringRepresentation);
    }
    
    private Dictionary<string, string> GetConfig()
    {
        return ConfigUtils.GetConfigSensitiveData("LdapSchemaLoaderTests.txt", "|");
    }
}