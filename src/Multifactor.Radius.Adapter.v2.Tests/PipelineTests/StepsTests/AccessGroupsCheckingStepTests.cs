using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Multifactor.Core.Ldap.Name;
using Multifactor.Core.Ldap.Schema;
using Multifactor.Radius.Adapter.v2.Core.Auth;
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
        var groupService = new Mock<ILdapGroupService>();
        var step = new AccessGroupsCheckingStep(groupService.Object, NullLogger<AccessGroupsCheckingStep>.Instance);
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
        var groupService = new Mock<ILdapGroupService>();
        var step = new AccessGroupsCheckingStep(groupService.Object, NullLogger<AccessGroupsCheckingStep>.Instance);
        
        await Assert.ThrowsAsync<ArgumentNullException>(() => step.ExecuteAsync(null));
    }
    
    [Fact]
    public async Task CheckAccessGroups_NoLdapServerConfiguration_ShouldThrow()
    {
        var groupService = new Mock<ILdapGroupService>();
        var step = new AccessGroupsCheckingStep(groupService.Object, NullLogger<AccessGroupsCheckingStep>.Instance);
        var contextMock = new Mock<IRadiusPipelineExecutionContext>();
        contextMock.Setup(x => x.Settings.LdapServerConfiguration).Returns(()=> null);
        var context = contextMock.Object;
        
        await Assert.ThrowsAsync<ArgumentNullException>(() => step.ExecuteAsync(context));
    }
    
    [Fact]
    public async Task CheckAccessGroups_NoUserLdapProfile_ShouldThrow()
    {
        var groupService = new Mock<ILdapGroupService>();
        var step = new AccessGroupsCheckingStep(groupService.Object, NullLogger<AccessGroupsCheckingStep>.Instance);
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
        var groupService = new Mock<ILdapGroupService>();
        var step = new AccessGroupsCheckingStep(groupService.Object, NullLogger<AccessGroupsCheckingStep>.Instance);
        var contextMock = new Mock<IRadiusPipelineExecutionContext>();
        var serverConfigMock = new Mock<ILdapServerConfiguration>();
        contextMock.Setup(x => x.Settings.LdapServerConfiguration).Returns(serverConfigMock.Object);
        contextMock.Setup(x => x.UserLdapProfile).Returns(() => new Mock<ILdapProfile>().Object);
        contextMock.Setup(x => x.LdapSchema).Returns(() => null);
        var context = contextMock.Object;
        
        await Assert.ThrowsAsync<ArgumentNullException>(() => step.ExecuteAsync(context));
    }

    [Fact]
    public async Task CheckAccessGroups_IsNotMember_ShouldTerminatePipeline()
    {
        var groupService = new Mock<ILdapGroupService>();
        groupService.Setup(x => x.IsMemberOf(It.IsAny<MembershipRequest>())).Returns(false);
        var step = new AccessGroupsCheckingStep(groupService.Object, NullLogger<AccessGroupsCheckingStep>.Instance);
        var contextMock = new Mock<IRadiusPipelineExecutionContext>();
        var serverConfigMock = new Mock<ILdapServerConfiguration>();
        var execState = new ExecutionState();
        serverConfigMock.Setup(x => x.AccessGroups).Returns(["dc=group,dc=admin,dc=user]"]);
        serverConfigMock.Setup(x => x.NestedGroupsBaseDns).Returns([]);
        serverConfigMock.Setup(x => x.ConnectionString).Returns("string");
        serverConfigMock.Setup(x => x.UserName).Returns("string");
        serverConfigMock.Setup(x => x.Password).Returns("string");
        serverConfigMock.Setup(x => x.BindTimeoutInSeconds).Returns(23);
        
        var profileMock = new Mock<ILdapProfile>();
        profileMock.Setup(x => x.Dn).Returns(new DistinguishedName("cn=admin,dc=admin,dc=user"));
        profileMock.Setup(x => x.MemberOf).Returns([]);
        
        contextMock.Setup(x => x.Settings.LdapServerConfiguration).Returns(serverConfigMock.Object);
        contextMock.Setup(x => x.UserLdapProfile).Returns(() => profileMock.Object);
        contextMock.Setup(x => x.LdapSchema).Returns(() => new Mock<ILdapSchema>().Object);
        contextMock.Setup(x => x.ExecutionState).Returns(execState);
        contextMock.SetupProperty(x => x.AuthenticationState);
        var context = contextMock.Object;
        context.AuthenticationState = new AuthenticationState();
        
        await step.ExecuteAsync(context);
        
        Assert.True(execState.IsTerminated);
        Assert.False(execState.ShouldSkipResponse);
    }
    
    [Fact]
    public async Task CheckAccessGroups_IsMember_ShouldNotTerminatePipeline()
    {
        var groupService = new Mock<ILdapGroupService>();
        groupService.Setup(x => x.IsMemberOf(It.IsAny<MembershipRequest>())).Returns(true);
        var step = new AccessGroupsCheckingStep(groupService.Object, NullLogger<AccessGroupsCheckingStep>.Instance);
        var contextMock = new Mock<IRadiusPipelineExecutionContext>();
        var serverConfigMock = new Mock<ILdapServerConfiguration>();

        serverConfigMock.Setup(x => x.AccessGroups).Returns(["dc=group,dc=admin,dc=user]"]);
        serverConfigMock.Setup(x => x.NestedGroupsBaseDns).Returns([]);
        serverConfigMock.Setup(x => x.ConnectionString).Returns("string");
        serverConfigMock.Setup(x => x.UserName).Returns("string");
        serverConfigMock.Setup(x => x.Password).Returns("string");
        serverConfigMock.Setup(x => x.BindTimeoutInSeconds).Returns(23);
        
        var profileMock = new Mock<ILdapProfile>();
        profileMock.Setup(x => x.Dn).Returns(new DistinguishedName("cn=admin,dc=admin,dc=user"));
        profileMock.Setup(x => x.MemberOf).Returns([]);
        contextMock.Setup(x => x.Settings.LdapServerConfiguration).Returns(serverConfigMock.Object);
        contextMock.Setup(x => x.UserLdapProfile).Returns(() => profileMock.Object);
        contextMock.Setup(x => x.LdapSchema).Returns(() => new Mock<ILdapSchema>().Object);
        
        var execState = new ExecutionState();
        contextMock.Setup(x => x.ExecutionState).Returns(execState);
        var context = contextMock.Object;
        
        await step.ExecuteAsync(context);
        
        Assert.False(execState.IsTerminated);
        Assert.False(execState.ShouldSkipResponse);
    }
}