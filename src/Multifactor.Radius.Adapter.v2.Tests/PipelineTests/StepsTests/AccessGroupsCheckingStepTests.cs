using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Multifactor.Core.Ldap.Connection;
using Multifactor.Core.Ldap.Name;
using Multifactor.Core.Ldap.Schema;
using Multifactor.Radius.Adapter.v2.Core.Configuration.Client;
using Multifactor.Radius.Adapter.v2.Core.Ldap;
using Multifactor.Radius.Adapter.v2.Core.Pipeline;
using Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline.Context;
using Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline.Steps;
using Multifactor.Radius.Adapter.v2.Services.Ldap;

namespace Multifactor.Radius.Adapter.v2.Tests.PipelineTests.StepsTests;

public class AccessGroupsCheckingStepTests
{
    [Fact]
    public async Task CheckAccessGroups_NoAccessGroups_ShouldComplete()
    {
        var connectionFactory = new Mock<ILdapConnectionFactory>();
        var groupService = new Mock<ILdapGroupService>();
        var step = new AccessGroupsCheckingStep(groupService.Object, connectionFactory.Object, NullLogger<AccessGroupsCheckingStep>.Instance);
        var contextMock = new Mock<IRadiusPipelineExecutionContext>();
        var serverConfigMock = new Mock<ILdapServerConfiguration>();
        var execState = new ExecutionState();
        serverConfigMock.Setup(x => x.AccessGroups).Returns([]);
        contextMock.Setup(x => x.Settings.LdapServerConfiguration).Returns(serverConfigMock.Object);
        contextMock.Setup(x => x.UserLdapProfile).Returns(() => new Mock<ILdapProfile>().Object);
        contextMock.Setup(x => x.LdapSchema).Returns(() => new Mock<ILdapSchema>().Object);
        contextMock.Setup(x => x.ExecutionState).Returns(execState);
        var context = contextMock.Object;
        
        await step.ExecuteAsync(context);
        
        Assert.False(execState.IsTerminated);
        Assert.False(execState.ShouldSkipResponse);
    }
    
    [Fact]
    public async Task CheckAccessGroups_NoContext_ShouldThrow()
    {
        var connectionFactory = new Mock<ILdapConnectionFactory>();
        var groupService = new Mock<ILdapGroupService>();
        var step = new AccessGroupsCheckingStep(groupService.Object, connectionFactory.Object, NullLogger<AccessGroupsCheckingStep>.Instance);
        
        await Assert.ThrowsAsync<ArgumentNullException>(() => step.ExecuteAsync(null));
    }
    
    [Fact]
    public async Task CheckAccessGroups_NoLdapServerConfiguration_ShouldThrow()
    {
        var connectionFactory = new Mock<ILdapConnectionFactory>();
        var groupService = new Mock<ILdapGroupService>();
        var step = new AccessGroupsCheckingStep(groupService.Object, connectionFactory.Object, NullLogger<AccessGroupsCheckingStep>.Instance);
        var contextMock = new Mock<IRadiusPipelineExecutionContext>();
        contextMock.Setup(x => x.Settings.LdapServerConfiguration).Returns(()=> null);
        var context = contextMock.Object;
        
        await Assert.ThrowsAsync<ArgumentNullException>(() => step.ExecuteAsync(context));
    }
    
    [Fact]
    public async Task CheckAccessGroups_NoUserLdapProfile_ShouldThrow()
    {
        var connectionFactory = new Mock<ILdapConnectionFactory>();
        var groupService = new Mock<ILdapGroupService>();
        var step = new AccessGroupsCheckingStep(groupService.Object, connectionFactory.Object, NullLogger<AccessGroupsCheckingStep>.Instance);
        var contextMock = new Mock<IRadiusPipelineExecutionContext>();
        var serverConfigMock = new Mock<ILdapServerConfiguration>();
        contextMock.Setup(x => x.Settings.LdapServerConfiguration).Returns(serverConfigMock.Object);
        contextMock.Setup(x => x.UserLdapProfile).Returns(() => null);
        contextMock.Setup(x => x.LdapSchema).Returns(() => new Mock<ILdapSchema>().Object);
        var context = contextMock.Object;
        
        await Assert.ThrowsAsync<ArgumentNullException>(() => step.ExecuteAsync(context));
    }
    
    [Fact]
    public async Task CheckAccessGroups_NoLdapSchema_ShouldThrow()
    {
        var connectionFactory = new Mock<ILdapConnectionFactory>();
        var groupService = new Mock<ILdapGroupService>();
        var step = new AccessGroupsCheckingStep(groupService.Object, connectionFactory.Object, NullLogger<AccessGroupsCheckingStep>.Instance);
        var contextMock = new Mock<IRadiusPipelineExecutionContext>();
        var serverConfigMock = new Mock<ILdapServerConfiguration>();
        contextMock.Setup(x => x.Settings.LdapServerConfiguration).Returns(serverConfigMock.Object);
        contextMock.Setup(x => x.UserLdapProfile).Returns(() => new Mock<ILdapProfile>().Object);
        contextMock.Setup(x => x.LdapSchema).Returns(() => null);
        var context = contextMock.Object;
        
        await Assert.ThrowsAsync<ArgumentNullException>(() => step.ExecuteAsync(context));
    }
    
    [Fact]
    public async Task CheckAccessGroups_LoadNestedGroupsIsFalse_ShouldNotTerminatePipeline()
    {
        var group = "dc=access, dc=group";
        var accessGroups = new [] {group};
        
        var serverConfigMock = new Mock<ILdapServerConfiguration>();
        serverConfigMock.Setup(x => x.LoadNestedGroups).Returns(false);
        serverConfigMock.Setup(x => x.AccessGroups).Returns(accessGroups);
        
        var contextMock = new Mock<IRadiusPipelineExecutionContext>();
        contextMock.Setup(x => x.Settings.LdapServerConfiguration).Returns(serverConfigMock.Object);
        contextMock.Setup(x => x.LdapSchema).Returns(() => new Mock<ILdapSchema>().Object);
        
        var profileMock  = new Mock<ILdapProfile>();
        profileMock.Setup(x => x.MemberOf).Returns([new DistinguishedName(group)]);
        contextMock.Setup(x => x.UserLdapProfile).Returns(() => profileMock.Object);
        
        var execState = new ExecutionState();
        contextMock.Setup(x => x.ExecutionState).Returns(execState);
        
        var groupServiceMock = new Mock<ILdapGroupService>();
        var connectionFactory = new Mock<ILdapConnectionFactory>();
        
        var step = new AccessGroupsCheckingStep(groupServiceMock.Object, connectionFactory.Object, NullLogger<AccessGroupsCheckingStep>.Instance);
        var context = contextMock.Object;
        await step.ExecuteAsync(context);
        
        Assert.False(execState.IsTerminated);
        Assert.False(execState.ShouldSkipResponse);
    }
    
    [Fact]
    public async Task CheckAccessGroups_LoadNestedGroupsIsFalse_ShouldTerminatePipeline()
    {
        var group = "dc=access, dc=group";
        var accessGroups = new [] {group};
        
        var contextMock = new Mock<IRadiusPipelineExecutionContext>();
        
        var serverConfigMock = new Mock<ILdapServerConfiguration>();
        serverConfigMock.Setup(x => x.LoadNestedGroups).Returns(false);
        serverConfigMock.Setup(x => x.AccessGroups).Returns(accessGroups);
        
        contextMock.Setup(x => x.Settings.LdapServerConfiguration).Returns(serverConfigMock.Object);
        contextMock.Setup(x => x.LdapSchema).Returns(() => new Mock<ILdapSchema>().Object);
        
        var profileMock  = new Mock<ILdapProfile>();
        profileMock.Setup(x => x.MemberOf).Returns([new DistinguishedName("dc=not, dc=access, dc=group")]);
        contextMock.Setup(x => x.UserLdapProfile).Returns(() => profileMock.Object);
        
        var execState = new ExecutionState();
        contextMock.Setup(x => x.ExecutionState).Returns(execState);
        
        var context = contextMock.Object;
        var connectionFactory = new Mock<ILdapConnectionFactory>();
        var groupServiceMock = new Mock<ILdapGroupService>();
        var step = new AccessGroupsCheckingStep(groupServiceMock.Object, connectionFactory.Object, NullLogger<AccessGroupsCheckingStep>.Instance);
        await step.ExecuteAsync(context);
        
        Assert.True(execState.IsTerminated);
        Assert.False(execState.ShouldSkipResponse);
    }
    
    [Fact]
    public async Task CheckAccessGroups_LoadNestedGroupsIsTrueNoBaseDns_ShouldNotTerminatePipeline()
    {
        var group = "dc=access, dc=group";
        var accessGroups = new [] {group};
        
        var connectionFactory = new Mock<ILdapConnectionFactory>();
        connectionFactory.Setup(x => x.CreateConnection(It.IsAny<LdapConnectionOptions>())).Returns(new Mock<ILdapConnection>().Object);
        
        var groupServiceMock = new Mock<ILdapGroupService>();
        groupServiceMock
            .Setup(x => x.IsMemberOf(
                It.IsAny<ILdapSchema>(),
                It.IsAny<ILdapConnection>(),
                It.IsAny<DistinguishedName>(),
                It.IsAny<DistinguishedName[]>(),
                It.IsAny<DistinguishedName?>()))
            .Returns(true);
        
        var serverConfigMock = new Mock<ILdapServerConfiguration>();
        serverConfigMock.Setup(x => x.LoadNestedGroups).Returns(true);
        serverConfigMock.Setup(x => x.ConnectionString).Returns("string");
        serverConfigMock.Setup(x => x.UserName).Returns("string");
        serverConfigMock.Setup(x => x.Password).Returns("string");
        serverConfigMock.Setup(x => x.BindTimeoutInSeconds).Returns(30);
        serverConfigMock.Setup(x => x.AccessGroups).Returns(accessGroups);
        serverConfigMock.Setup(x => x.NestedGroupsBaseDns).Returns([]);
        
        var contextMock = new Mock<IRadiusPipelineExecutionContext>();
        contextMock.Setup(x => x.Settings.LdapServerConfiguration).Returns(serverConfigMock.Object);
        contextMock.Setup(x => x.LdapSchema).Returns(() => new Mock<ILdapSchema>().Object);
        
        var profileMock  = new Mock<ILdapProfile>();
        profileMock.Setup(x => x.MemberOf).Returns([new DistinguishedName("dc=not, dc=access, dc=group")]);
        profileMock.Setup(x => x.Dn).Returns(new DistinguishedName("dc=user, dc=domain"));
        contextMock.Setup(x => x.UserLdapProfile).Returns(() => profileMock.Object);
        
        var execState = new ExecutionState();
        contextMock.Setup(x => x.ExecutionState).Returns(execState);
        
        var context = contextMock.Object;
        
        var step = new AccessGroupsCheckingStep(groupServiceMock.Object, connectionFactory.Object, NullLogger<AccessGroupsCheckingStep>.Instance);
        await step.ExecuteAsync(context);
        
        Assert.False(execState.IsTerminated);
        Assert.False(execState.ShouldSkipResponse);
    }
    
    [Fact]
    public async Task CheckAccessGroups_LoadNestedGroupsIsTrueHasBaseDns_ShouldNotTerminatePipeline()
    {
        var group = "dc=access, dc=group";
        
        var connectionFactory = new Mock<ILdapConnectionFactory>();
        connectionFactory.Setup(x => x.CreateConnection(It.IsAny<LdapConnectionOptions>())).Returns(new Mock<ILdapConnection>().Object);
        
        var groupServiceMock = new Mock<ILdapGroupService>();
        groupServiceMock
            .Setup(x => x.IsMemberOf(
                It.IsAny<ILdapSchema>(),
                It.IsAny<ILdapConnection>(),
                It.IsAny<DistinguishedName>(),
                It.IsAny<DistinguishedName[]>(),
                It.IsAny<DistinguishedName?>()))
            .Returns(true);
        
        var contextMock = new Mock<IRadiusPipelineExecutionContext>();
        
        var serverConfigMock = new Mock<ILdapServerConfiguration>();
        serverConfigMock.Setup(x => x.LoadNestedGroups).Returns(true);
        serverConfigMock.Setup(x => x.ConnectionString).Returns("string");
        serverConfigMock.Setup(x => x.UserName).Returns("string");
        serverConfigMock.Setup(x => x.Password).Returns("string");
        serverConfigMock.Setup(x => x.BindTimeoutInSeconds).Returns(30);
        serverConfigMock.Setup(x => x.AccessGroups).Returns([group]);
        serverConfigMock.Setup(x => x.NestedGroupsBaseDns).Returns(["dc=group, dc=dn"]);
        
        contextMock.Setup(x => x.Settings.LdapServerConfiguration).Returns(serverConfigMock.Object);
        contextMock.Setup(x => x.LdapSchema).Returns(() => new Mock<ILdapSchema>().Object);
        
        var profileMock  = new Mock<ILdapProfile>();
        profileMock.Setup(x => x.MemberOf).Returns([new DistinguishedName("dc=not, dc=access, dc=group")]);
        profileMock.Setup(x => x.Dn).Returns(new DistinguishedName("dc=user, dc=domain"));
        contextMock.Setup(x => x.UserLdapProfile).Returns(() => profileMock.Object);
        
        var execState = new ExecutionState();
        contextMock.Setup(x => x.ExecutionState).Returns(execState);
        
        var context = contextMock.Object;
        
        var step = new AccessGroupsCheckingStep(groupServiceMock.Object, connectionFactory.Object, NullLogger<AccessGroupsCheckingStep>.Instance);
        await step.ExecuteAsync(context);
        
        Assert.False(execState.IsTerminated);
        Assert.False(execState.ShouldSkipResponse);
    }
    
    [Fact]
    public async Task CheckAccessGroups_LoadNestedGroupsIsTrueNoBaseDns_ShouldTerminatePipeline()
    {
        var group = "dc=access, dc=group";
        
        var connectionFactory = new Mock<ILdapConnectionFactory>();
        connectionFactory.Setup(x => x.CreateConnection(It.IsAny<LdapConnectionOptions>())).Returns(new Mock<ILdapConnection>().Object);
        
        var groupServiceMock = new Mock<ILdapGroupService>();
        groupServiceMock
            .Setup(x => x.IsMemberOf(
                It.IsAny<ILdapSchema>(),
                It.IsAny<ILdapConnection>(),
                It.IsAny<DistinguishedName>(),
                It.IsAny<DistinguishedName[]>(),
                It.IsAny<DistinguishedName?>()))
            .Returns(false);
        
        var contextMock = new Mock<IRadiusPipelineExecutionContext>();
        
        var serverConfigMock = new Mock<ILdapServerConfiguration>();
        serverConfigMock.Setup(x => x.LoadNestedGroups).Returns(true);
        serverConfigMock.Setup(x => x.ConnectionString).Returns("string");
        serverConfigMock.Setup(x => x.UserName).Returns("string");
        serverConfigMock.Setup(x => x.Password).Returns("string");
        serverConfigMock.Setup(x => x.BindTimeoutInSeconds).Returns(30);
        serverConfigMock.Setup(x => x.AccessGroups).Returns([group]);
        serverConfigMock.Setup(x => x.NestedGroupsBaseDns).Returns([]);
        
        contextMock.Setup(x => x.Settings.LdapServerConfiguration).Returns(serverConfigMock.Object);
        contextMock.Setup(x => x.LdapSchema).Returns(() => new Mock<ILdapSchema>().Object);
        
        var profileMock  = new Mock<ILdapProfile>();
        profileMock.Setup(x => x.MemberOf).Returns([new DistinguishedName("dc=not, dc=access, dc=group")]);
        profileMock.Setup(x => x.Dn).Returns(new DistinguishedName("dc=user, dc=domain"));
        contextMock.Setup(x => x.UserLdapProfile).Returns(() => profileMock.Object);
        
        var execState = new ExecutionState();
        contextMock.Setup(x => x.ExecutionState).Returns(execState);
        
        var context = contextMock.Object;
        
        var step = new AccessGroupsCheckingStep(groupServiceMock.Object, connectionFactory.Object, NullLogger<AccessGroupsCheckingStep>.Instance);
        await step.ExecuteAsync(context);
        
        Assert.True(execState.IsTerminated);
        Assert.False(execState.ShouldSkipResponse);
    }
    
    [Fact]
    public async Task CheckAccessGroups_LoadNestedGroupsIsTrueHasBaseDns_ShouldTerminatePipeline()
    {
        var group = "dc=access, dc=group";

        var connectionFactory = new Mock<ILdapConnectionFactory>();
        connectionFactory.Setup(x => x.CreateConnection(It.IsAny<LdapConnectionOptions>())).Returns(new Mock<ILdapConnection>().Object);
        
        var groupServiceMock = new Mock<ILdapGroupService>();
        groupServiceMock
            .Setup(x => x.IsMemberOf(
                It.IsAny<ILdapSchema>(),
                It.IsAny<ILdapConnection>(),
                It.IsAny<DistinguishedName>(),
                It.IsAny<DistinguishedName[]>(),
                It.IsAny<DistinguishedName?>()))
            .Returns(false);
        
        var contextMock = new Mock<IRadiusPipelineExecutionContext>();
        
        var serverConfigMock = new Mock<ILdapServerConfiguration>();
        serverConfigMock.Setup(x => x.LoadNestedGroups).Returns(true);
        serverConfigMock.Setup(x => x.ConnectionString).Returns("string");
        serverConfigMock.Setup(x => x.UserName).Returns("string");
        serverConfigMock.Setup(x => x.Password).Returns("string");
        serverConfigMock.Setup(x => x.BindTimeoutInSeconds).Returns(30);
        serverConfigMock.Setup(x => x.AccessGroups).Returns([group]);
        serverConfigMock.Setup(x => x.NestedGroupsBaseDns).Returns(["dc=group, dc=dn"]);
        
        contextMock.Setup(x => x.Settings.LdapServerConfiguration).Returns(serverConfigMock.Object);
        contextMock.Setup(x => x.LdapSchema).Returns(() => new Mock<ILdapSchema>().Object);
        
        var profileMock  = new Mock<ILdapProfile>();
        profileMock.Setup(x => x.MemberOf).Returns([new DistinguishedName("dc=not, dc=access, dc=group")]);
        profileMock.Setup(x => x.Dn).Returns(new DistinguishedName("dc=user, dc=domain"));
        contextMock.Setup(x => x.UserLdapProfile).Returns(() => profileMock.Object);
        
        var execState = new ExecutionState();
        contextMock.Setup(x => x.ExecutionState).Returns(execState);
        
        var context = contextMock.Object;
        
        var step = new AccessGroupsCheckingStep(groupServiceMock.Object, connectionFactory.Object, NullLogger<AccessGroupsCheckingStep>.Instance);
        await step.ExecuteAsync(context);
        
        Assert.True(execState.IsTerminated);
        Assert.False(execState.ShouldSkipResponse);
    }
}