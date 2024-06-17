using System.Net;
using Moq;
using MultiFactor.Radius.Adapter.Core.Framework.Context;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration.ClientLevel;
using MultiFactor.Radius.Adapter.Services.Ldap.Profile;
using MultiFactor.Radius.Adapter.Tests.Fixtures.Radius;

namespace MultiFactor.Radius.Adapter.Tests;

public class RadiusReplyAttributeValueTests
{
    [Theory]
    [InlineData("888")]
    [InlineData("VPN Admins", "888")]
    public void IsMatch_SingleUserGroups_ShouldReturnTrue(params string[] groups)
    {
        var client = new ClientConfiguration("cli_config", "rds", AuthenticationSource.None, "key", "secret");
        var context = new RadiusContext(RadiusPacketFactory.AccessRequest(), client, new Mock<IServiceProvider>().Object)
        {
            RemoteEndpoint = new IPEndPoint(IPAddress.Any, 636)
        };
            
        var memberof = new LdapAttributes("dn", new Dictionary<string, string[]>
        {
            { "memberOf", groups }
        });
        context.Profile.UpdateAttributes(memberof);
            
        var attribute = new RadiusReplyAttributeValue("Admins", "UserGroup=888");
            
        Assert.True(attribute.IsMatch(context));
    }
    
    [Theory]
    [InlineData("VPN Admins", "888")]
    public void IsMatch_SingleUserGroups_ShouldReturnFalse(params string[] groups)
    {
        var client = new ClientConfiguration("cli_config", "rds", AuthenticationSource.None, "key", "secret");
        var context = new RadiusContext(RadiusPacketFactory.AccessRequest(), client, new Mock<IServiceProvider>().Object)
        {
            RemoteEndpoint = new IPEndPoint(IPAddress.Any, 636)
        };
            
        var memberof = new LdapAttributes("dn", new Dictionary<string, string[]>
        {
            { "memberOf", groups }
        });
        context.Profile.UpdateAttributes(memberof);
            
        var attribute = new RadiusReplyAttributeValue("Admins", "UserGroup=VPNAdmins");
            
        Assert.False(attribute.IsMatch(context));
    }
    
    [Theory]
    [InlineData("VPN Admins")]
    [InlineData("888")]
    [InlineData("VPN Admins", "888")]
    [InlineData("Some Group", "VPN Admins", "888")]
    public void IsMatch_MultipleUserGroups_ShouldReturnTrue(params string[] groups)
    {
        var client = new ClientConfiguration("cli_config", "rds", AuthenticationSource.None, "key", "secret");
        var context = new RadiusContext(RadiusPacketFactory.AccessRequest(), client, new Mock<IServiceProvider>().Object)
        {
            RemoteEndpoint = new IPEndPoint(IPAddress.Any, 636)
        };
            
        var memberof = new LdapAttributes("dn", new Dictionary<string, string[]>
        {
            { "memberOf", groups }
        });
        context.Profile.UpdateAttributes(memberof);
            
        var attribute = new RadiusReplyAttributeValue("Admins", "UserGroup=VPN Admins;888");
            
        Assert.True(attribute.IsMatch(context));
    }
}