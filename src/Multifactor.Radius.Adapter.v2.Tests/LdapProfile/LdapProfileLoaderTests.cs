using System.DirectoryServices.Protocols;
using Multifactor.Core.Ldap;
using Multifactor.Core.Ldap.Connection;
using Multifactor.Core.Ldap.Connection.LdapConnectionFactory;
using Multifactor.Core.Ldap.Name;
using Multifactor.Core.Ldap.Schema;
using Multifactor.Radius.Adapter.v2.Services.Ldap;
using Multifactor.Radius.Adapter.v2.Tests.Fixture;

namespace Multifactor.Radius.Adapter.v2.Tests.LdapProfile;

public class LdapProfileLoaderTests
{
    [Fact]
    public void LoadProfile_ShouldLoadProfile()
    {
        var factory = LdapConnectionFactory.Create();

        var sensitiveData = GetConfig();
        var options = new LdapConnectionOptions(
            new LdapConnectionString(sensitiveData["ConnectionString"]),
            AuthType.Basic,
            sensitiveData["Admin"],
            sensitiveData["AdminPwd"]);
        var connection = factory.CreateConnection(options);
        
        var searchBase = new DistinguishedName(sensitiveData["SearchBase"]);
        var schema =  LdapSchemaBuilder.Create();
        schema.LdapServerImplementation = LdapImplementation.ActiveDirectory;
        var loader = new LdapProfileLoader(searchBase,connection,schema);

        var filter = $"(&(objectClass={sensitiveData["ObjectClass"]})({sensitiveData["IdentityAttribute1"]}={sensitiveData["TargetUserDn"]}))";
        var profile = loader.LoadLdapProfile(filter);
        Assert.NotNull(profile);
        var expectedDn = new DistinguishedName(sensitiveData["TargetUserDn"]);
        Assert.Equal(expectedDn, profile.Dn);
    }
    
    private Dictionary<string, string> GetConfig()
    {
        return ConfigUtils.GetConfigSensitiveData("LoadProfile.txt", "|");
    }
}