using System.DirectoryServices.Protocols;
using Moq;
using Multifactor.Core.Ldap;
using Multifactor.Core.Ldap.Connection;
using Multifactor.Core.Ldap.Connection.LdapConnectionFactory;
using Multifactor.Core.Ldap.Name;
using Multifactor.Core.Ldap.Schema;
using Multifactor.Radius.Adapter.v2.Core.Ldap;
using Multifactor.Radius.Adapter.v2.Services.Ldap;
using Multifactor.Radius.Adapter.v2.Tests.Fixture;

namespace Multifactor.Radius.Adapter.v2.Tests;

[Collection("ActiveDirectory")]
public class LdapPasswordChangerTests
{
    [Fact]
    public async Task ChangePassword_ShouldChange()
    {
        var factory = LdapConnectionFactory.Create();

        var sensitiveData = GetConfig();

        var options = new LdapConnectionOptions(
            new LdapConnectionString(sensitiveData["ConnectionString"]),
            AuthType.Basic,
            sensitiveData["Admin"],
            sensitiveData["AdminPwd"]);

        using var adminConnection = factory.CreateConnection(options);
        var schema = LdapSchemaBuilder.Create();
        schema.LdapServerImplementation = LdapImplementation.ActiveDirectory;
        var changer = new LdapPasswordChanger(adminConnection, schema);
        var profileMock = new Mock<ILdapProfile>();
        profileMock.Setup(x => x.Dn).Returns(new DistinguishedName(sensitiveData["UserDn"]));
        var response = await changer.ChangeUserPasswordAsync(sensitiveData["NewPassword"], profileMock.Object);
        
        Assert.NotNull(response);
        Assert.True(response.Success);
        
        var userConnectionOptions = new LdapConnectionOptions(
            new LdapConnectionString(sensitiveData["ConnectionString"]),
            AuthType.Basic,
            sensitiveData["UserName"],
            sensitiveData["NewPassword"]);
        using var newPasswordConnection = factory.CreateConnection(userConnectionOptions);
        
        //Rollback
        response = await changer.ChangeUserPasswordAsync(sensitiveData["CurrentPassword"], profileMock.Object);
        Assert.NotNull(response);
        Assert.True(response.Success);
        
        userConnectionOptions = new LdapConnectionOptions(
            new LdapConnectionString(sensitiveData["ConnectionString"]),
            AuthType.Basic,
            sensitiveData["UserName"],
            sensitiveData["CurrentPassword"]);
        
        using var oldPasswordConnection = factory.CreateConnection(userConnectionOptions);
    }
    
    [Fact]
    public async Task ChangePassword_UnsuccessfulResponseCode_ShouldFailed()
    {
        var factory = LdapConnectionFactory.Create();

        var sensitiveData = GetConfig();

        var options = new LdapConnectionOptions(
            new LdapConnectionString(sensitiveData["ConnectionString"]),
            AuthType.Basic,
            sensitiveData["UserDn"],
            sensitiveData["CurrentPassword"]);

        using var adminConnection = factory.CreateConnection(options);
        var schema = LdapSchemaBuilder.Create();
        schema.LdapServerImplementation = LdapImplementation.ActiveDirectory;
        var changer = new LdapPasswordChanger(adminConnection, schema);
        var profileMock = new Mock<ILdapProfile>();
        profileMock.Setup(x => x.Dn).Returns(new DistinguishedName(sensitiveData["UserDn"]));
        var response = await changer.ChangeUserPasswordAsync(sensitiveData["NewPassword"], profileMock.Object);
        
        Assert.NotNull(response);
        Assert.False(response.Success);
        Assert.NotNull(response.Message);
        Assert.NotEmpty(response.Message);
        
        var userConnectionOptions = new LdapConnectionOptions(
            new LdapConnectionString(sensitiveData["ConnectionString"]),
            AuthType.Basic,
            sensitiveData["UserName"],
            sensitiveData["NewPassword"]);
        
       Assert.ThrowsAny<LdapException>(() => factory.CreateConnection(userConnectionOptions));
    }

    private Dictionary<string, string> GetConfig()
    {
        return ConfigUtils.GetConfigSensitiveData("ChangePasswordTests.txt", "|");
    }
}