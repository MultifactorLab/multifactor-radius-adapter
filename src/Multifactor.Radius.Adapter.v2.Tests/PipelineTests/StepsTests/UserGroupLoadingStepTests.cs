using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Multifactor.Core.Ldap.Connection;
using Multifactor.Core.Ldap.Name;
using Multifactor.Core.Ldap.Schema;
using Multifactor.Radius.Adapter.v2.Core.Configuration.Client;
using Multifactor.Radius.Adapter.v2.Core.Ldap;
using Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline.Context;
using Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline.Steps;
using Multifactor.Radius.Adapter.v2.Services.Ldap;

namespace Multifactor.Radius.Adapter.v2.Tests.PipelineTests.StepsTests;

public class UserGroupLoadingStepTests
{
    [Fact]
    public async Task LoadGroups_NoReplyAttributes_ShouldSkipGroupLoading()
    {
        var groupService = new Mock<ILdapGroupService>();
        var connectionFactory = new Mock<ILdapConnectionFactory>();

        var step = new UserGroupLoadingStep(groupService.Object, connectionFactory.Object, NullLogger<UserGroupLoadingStep>.Instance);
        var contextMock = new Mock<IRadiusPipelineExecutionContext>();
        contextMock.Setup(x => x.RadiusReplyAttributes).Returns(new Dictionary<string, RadiusReplyAttributeValue[]>());
        
        contextMock.SetupProperty(x => x.UserGroups);
        var context = contextMock.Object;
        context.UserGroups = new();
        
        await step.ExecuteAsync(context);
        
        Assert.Empty(context.UserGroups);
    }
    
    [Fact]
    public async Task LoadGroups_NoRequiredAttributes_ShouldSkipGroupLoading()
    {
        var groupService = new Mock<ILdapGroupService>();
        var connectionFactory = new Mock<ILdapConnectionFactory>();

        var step = new UserGroupLoadingStep(groupService.Object, connectionFactory.Object, NullLogger<UserGroupLoadingStep>.Instance);
        var contextMock = new Mock<IRadiusPipelineExecutionContext>();
        var attributes = new Dictionary<string, RadiusReplyAttributeValue[]>();
        attributes.Add("key", [new RadiusReplyAttributeValue("name", string.Empty)]);
        contextMock.Setup(x => x.RadiusReplyAttributes).Returns(attributes);
        
        contextMock.SetupProperty(x => x.UserGroups);
        var context = contextMock.Object;
        context.UserGroups = new();
        
        await step.ExecuteAsync(context);
        
        Assert.Empty(context.UserGroups);
    }
    
    [Theory]
    [InlineData("dc=group1, dc=domain")]
    [InlineData("dc=group1, dc=domain;dc=group2, dc=domain")]
    [InlineData("dc=group1, dc=domain;dc=group2, dc=domain;dc=group3, dc=domain")]
    public async Task LoadGroups_NestedGroupsNotRequired_ShouldGetMemberOfValues(string groups)
    {
        var groupService = new Mock<ILdapGroupService>();
        var connectionFactory = new Mock<ILdapConnectionFactory>();
        var attributes = new Dictionary<string, RadiusReplyAttributeValue[]>();
        attributes.Add("key", [new RadiusReplyAttributeValue("name", "UserGroup=group1")]);
        
        var memberOf = groups.Split(';').Select(x => new DistinguishedName(x)).ToList();
        
        var step = new UserGroupLoadingStep(groupService.Object, connectionFactory.Object, NullLogger<UserGroupLoadingStep>.Instance);
        var contextMock = new Mock<IRadiusPipelineExecutionContext>();
        contextMock.Setup(x => x.RadiusReplyAttributes).Returns(attributes);
        contextMock.Setup(x => x.UserLdapProfile.MemberOf).Returns(memberOf);
        contextMock.SetupProperty(x => x.UserGroups);
        contextMock.Setup(x => x.LdapServerConfiguration.LoadNestedGroups).Returns(false);
        
        var context = contextMock.Object;
        context.UserGroups = new();
        
        await step.ExecuteAsync(context);
        
        Assert.NotEmpty(context.UserGroups);
        Assert.True(memberOf.Select(x => x.Components.Deepest.Value).SequenceEqual(context.UserGroups));
    }
    
    [Theory]
    [InlineData("dc=group1, dc=domain")]
    [InlineData("dc=group1, dc=domain;dc=group2, dc=domain")]
    [InlineData("dc=group1, dc=domain;dc=group2, dc=domain;dc=group3, dc=domain")]
    public async Task LoadGroups_NestedGroupsRequired_ShouldGetMemberOfValues(string groups)
    {
        var groupService = new Mock<ILdapGroupService>();
        groupService.Setup(x => x.LoadUserGroups(It.IsAny<LoadUserGroupsRequest>())).Returns([]);
        
        var connectionFactory = new Mock<ILdapConnectionFactory>();
        connectionFactory.Setup(x => x.CreateConnection(It.IsAny<LdapConnectionOptions>())).Returns(new Mock<ILdapConnection>().Object);
        
        var attributes = new Dictionary<string, RadiusReplyAttributeValue[]>();
        attributes.Add("key", [new RadiusReplyAttributeValue("name", "UserGroup=group1")]);
        
        var memberOf = groups.Split(';').Select(x => new DistinguishedName(x)).ToList();
        
        var step = new UserGroupLoadingStep(groupService.Object, connectionFactory.Object, NullLogger<UserGroupLoadingStep>.Instance);
        var contextMock = new Mock<IRadiusPipelineExecutionContext>();
        contextMock.Setup(x => x.RadiusReplyAttributes).Returns(attributes);
        contextMock.Setup(x => x.UserLdapProfile.MemberOf).Returns(memberOf);
        contextMock.SetupProperty(x => x.UserGroups);
        contextMock.Setup(x => x.LdapServerConfiguration.NestedGroupsBaseDns).Returns([]);
        contextMock.Setup(x => x.LdapServerConfiguration.LoadNestedGroups).Returns(true);
        contextMock.Setup(x => x.LdapServerConfiguration.UserName).Returns("user");
        contextMock.Setup(x => x.LdapServerConfiguration.Password).Returns("password");
        contextMock.Setup(x => x.LdapServerConfiguration.BindTimeoutInSeconds).Returns(30);
        contextMock.Setup(x => x.LdapSchema).Returns(new Mock<ILdapSchema>().Object);
        contextMock.Setup(x => x.UserLdapProfile.Dn).Returns(new DistinguishedName("dc=group1, dc=domain"));
        contextMock.Setup(x => x.LdapServerConfiguration.ConnectionString).Returns("Server=localhost;Port=5432");
        contextMock.Setup(x => x.RequestPacket.UserName).Returns("userName");
        
        var context = contextMock.Object;
        context.UserGroups = new();
        
        await step.ExecuteAsync(context);
        
        Assert.NotEmpty(context.UserGroups);
        Assert.True(memberOf.Select(x => x.Components.Deepest.Value).SequenceEqual(context.UserGroups));
    }
    
    [Theory]
    [InlineData("group1")]
    [InlineData("group1;group2")]
    [InlineData("group1;group2;group3")]
    public async Task LoadGroups_GroupsFromRoot_ShouldGetUserGroups(string groups)
    {
        var expectedGroups = groups.Split(';').ToList();
        var groupService = new Mock<ILdapGroupService>();
        groupService.Setup(x => x.LoadUserGroups(It.IsAny<LoadUserGroupsRequest>())).Returns(expectedGroups);
        
        var connectionFactory = new Mock<ILdapConnectionFactory>();
        connectionFactory.Setup(x => x.CreateConnection(It.IsAny<LdapConnectionOptions>())).Returns(new Mock<ILdapConnection>().Object);
        
        var attributes = new Dictionary<string, RadiusReplyAttributeValue[]>();
        attributes.Add("key", [new RadiusReplyAttributeValue("name", "UserGroup=group1")]);
        
        var step = new UserGroupLoadingStep(groupService.Object, connectionFactory.Object, NullLogger<UserGroupLoadingStep>.Instance);
        var contextMock = new Mock<IRadiusPipelineExecutionContext>();
        contextMock.Setup(x => x.RadiusReplyAttributes).Returns(attributes);
        contextMock.Setup(x => x.UserLdapProfile.MemberOf).Returns([]);
        contextMock.SetupProperty(x => x.UserGroups);
        contextMock.Setup(x => x.LdapServerConfiguration.NestedGroupsBaseDns).Returns([]);
        contextMock.Setup(x => x.LdapServerConfiguration.LoadNestedGroups).Returns(true);
        contextMock.Setup(x => x.LdapServerConfiguration.UserName).Returns("user");
        contextMock.Setup(x => x.LdapServerConfiguration.Password).Returns("password");
        contextMock.Setup(x => x.LdapServerConfiguration.BindTimeoutInSeconds).Returns(30);
        contextMock.Setup(x => x.LdapSchema).Returns(new Mock<ILdapSchema>().Object);
        contextMock.Setup(x => x.UserLdapProfile.Dn).Returns(new DistinguishedName("dc=group1, dc=domain"));
        contextMock.Setup(x => x.LdapServerConfiguration.ConnectionString).Returns("Server=localhost;Port=5432");
        contextMock.Setup(x => x.RequestPacket.UserName).Returns("userName");
        
        var context = contextMock.Object;
        context.UserGroups = new();
        
        await step.ExecuteAsync(context);
        
        Assert.NotEmpty(context.UserGroups);
        Assert.True(expectedGroups.SequenceEqual(context.UserGroups));
    }
    
    [Theory]
    [InlineData("group1")]
    [InlineData("group1;group2")]
    [InlineData("group1;group2;group3")]
    public async Task LoadGroups_GroupsFromContainers_ShouldGetUserGroups(string groups)
    {
        var expectedGroups = groups.Split(';').ToList();
        var groupService = new Mock<ILdapGroupService>();
        groupService.Setup(x => x.LoadUserGroups(It.IsAny<LoadUserGroupsRequest>())).Returns(expectedGroups);
        
        var connectionFactory = new Mock<ILdapConnectionFactory>();
        connectionFactory.Setup(x => x.CreateConnection(It.IsAny<LdapConnectionOptions>())).Returns(new Mock<ILdapConnection>().Object);
        
        var attributes = new Dictionary<string, RadiusReplyAttributeValue[]>();
        attributes.Add("key", [new RadiusReplyAttributeValue("name", "UserGroup=group1")]);
        
        var step = new UserGroupLoadingStep(groupService.Object, connectionFactory.Object, NullLogger<UserGroupLoadingStep>.Instance);
        var contextMock = new Mock<IRadiusPipelineExecutionContext>();
        contextMock.Setup(x => x.RadiusReplyAttributes).Returns(attributes);
        contextMock.Setup(x => x.UserLdapProfile.MemberOf).Returns([]);
        contextMock.SetupProperty(x => x.UserGroups);
        contextMock.Setup(x => x.LdapServerConfiguration.NestedGroupsBaseDns).Returns(["dc=nested,dc=group"]);
        contextMock.Setup(x => x.LdapServerConfiguration.LoadNestedGroups).Returns(true);
        contextMock.Setup(x => x.LdapServerConfiguration.UserName).Returns("user");
        contextMock.Setup(x => x.LdapServerConfiguration.Password).Returns("password");
        contextMock.Setup(x => x.LdapServerConfiguration.BindTimeoutInSeconds).Returns(30);
        contextMock.Setup(x => x.LdapSchema).Returns(new Mock<ILdapSchema>().Object);
        contextMock.Setup(x => x.UserLdapProfile.Dn).Returns(new DistinguishedName("dc=group1, dc=domain"));
        contextMock.Setup(x => x.LdapServerConfiguration.ConnectionString).Returns("Server=localhost;Port=5432");
        contextMock.Setup(x => x.RequestPacket.UserName).Returns("userName");
        
        var context = contextMock.Object;
        context.UserGroups = new();
        
        await step.ExecuteAsync(context);
        
        Assert.NotEmpty(context.UserGroups);
        Assert.True(expectedGroups.SequenceEqual(context.UserGroups));
    }
}