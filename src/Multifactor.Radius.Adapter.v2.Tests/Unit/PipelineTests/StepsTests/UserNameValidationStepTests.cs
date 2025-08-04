using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Multifactor.Radius.Adapter.v2.Core.Auth;
using Multifactor.Radius.Adapter.v2.Core.Configuration.Client;
using Multifactor.Radius.Adapter.v2.Core.Pipeline;
using Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline.Context;
using Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline.Steps;
using Multifactor.Radius.Adapter.v2.Tests.Fixture;

namespace Multifactor.Radius.Adapter.v2.Tests.Unit.PipelineTests.StepsTests;

public class UserNameValidationStepTests
{
    [Theory]
    [ClassData(typeof(EmptyStringsListInput))]
    public async Task ExecuteAsync_EmptyUserName_ShouldThrow(string userName)
    {
        //Arrange
        var step = new UserNameValidationStep(NullLogger<UserNameValidationStep>.Instance);
        var context = new Mock<IRadiusPipelineExecutionContext>();
        context.Setup(x=> x.RequestPacket.UserName).Returns(userName);

        //Act
        //Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => step.ExecuteAsync(context.Object));
    }
    
    [Fact]
    public async Task ExecuteAsync_NoServerConfiguration_ShouldThrow()
    {
        //Arrange
        var step = new UserNameValidationStep(NullLogger<UserNameValidationStep>.Instance);
        var context = new Mock<IRadiusPipelineExecutionContext>();
        context.Setup(x=> x.RequestPacket.UserName).Returns("userName");
        context.Setup(x=> x.LdapServerConfiguration).Returns(() => null);

        //Act
        //Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => step.ExecuteAsync(context.Object));
    }
    
    [Theory]
    [InlineData("name")]
    [InlineData("domain/name")]
    [InlineData("cn=user,dc=domain,dc=com")]
    public async Task ExecuteAsync_UpnRequiredAndUserNameNotUpn_ShouldTerminatePipeline(string userName)
    {
        //Arrange
        var step = new UserNameValidationStep(NullLogger<UserNameValidationStep>.Instance);
        var serverConfigMock = new Mock<ILdapServerConfiguration>();
        serverConfigMock.Setup(x => x.UpnRequired).Returns(true);
        var execState = new ExecutionState();
        var authState = new AuthenticationState();
        var responseInfo = new ResponseInformation();
        var contextMock = new Mock<IRadiusPipelineExecutionContext>();
        contextMock.Setup(x=> x.RequestPacket.UserName).Returns(userName);
        contextMock.Setup(x=> x.LdapServerConfiguration).Returns(serverConfigMock.Object);
        contextMock.Setup(x => x.ResponseInformation).Returns(responseInfo);
        contextMock.Setup(x => x.AuthenticationState).Returns(authState);
        contextMock.Setup(x => x.ExecutionState).Returns(execState);
        var context = contextMock.Object;

        //Act
        await step.ExecuteAsync(context);
        
        //Assert
        Assert.True(execState.IsTerminated);
        Assert.Equal(AuthenticationStatus.Reject, authState.FirstFactorStatus);
        Assert.Equal(AuthenticationStatus.Reject, authState.SecondFactorStatus);
    }
    
    [Theory]
    [InlineData("name")]
    [InlineData("domain/name")]
    [InlineData("cn=user,dc=domain,dc=com")]
    public async Task ExecuteAsync_UpnNotRequiredAndUserNameNotUpn_ShouldCompleteStep(string userName)
    {
        //Arrange
        var step = new UserNameValidationStep(NullLogger<UserNameValidationStep>.Instance);
        var serverConfigMock = new Mock<ILdapServerConfiguration>();
        serverConfigMock.Setup(x => x.UpnRequired).Returns(false);
        var execState = new ExecutionState();
        var authState = new AuthenticationState();
        var responseInfo = new ResponseInformation();
        var contextMock = new Mock<IRadiusPipelineExecutionContext>();
        contextMock.Setup(x=> x.RequestPacket.UserName).Returns(userName);
        contextMock.Setup(x=> x.LdapServerConfiguration).Returns(serverConfigMock.Object);
        contextMock.Setup(x => x.ResponseInformation).Returns(responseInfo);
        contextMock.Setup(x => x.AuthenticationState).Returns(authState);
        contextMock.Setup(x => x.ExecutionState).Returns(execState);
        var context = contextMock.Object;

        //Act
        await step.ExecuteAsync(context);
        
        //Assert
        Assert.False(execState.IsTerminated);
        Assert.Equal(AuthenticationStatus.Awaiting, authState.FirstFactorStatus);
        Assert.Equal(AuthenticationStatus.Awaiting, authState.SecondFactorStatus);
    }
    
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExecuteAsync_UserNameSuffixPermitted_ShouldCompleteStep(bool upnRequired)
    {
        //Arrange
        var step = new UserNameValidationStep(NullLogger<UserNameValidationStep>.Instance);
        var serverConfigMock = new Mock<ILdapServerConfiguration>();
        serverConfigMock.Setup(x => x.UpnRequired).Returns(upnRequired);
        serverConfigMock.Setup(x => x.SuffixesPermissions).Returns(new PermissionRules());
        var execState = new ExecutionState();
        var authState = new AuthenticationState();
        var responseInfo = new ResponseInformation();
        var contextMock = new Mock<IRadiusPipelineExecutionContext>();
        contextMock.Setup(x=> x.RequestPacket.UserName).Returns("user@domain.com");
        contextMock.Setup(x=> x.LdapServerConfiguration).Returns(serverConfigMock.Object);
        contextMock.Setup(x => x.ResponseInformation).Returns(responseInfo);
        contextMock.Setup(x => x.AuthenticationState).Returns(authState);
        contextMock.Setup(x => x.ExecutionState).Returns(execState);
        var context = contextMock.Object;

        //Act
        await step.ExecuteAsync(context);
        
        //Assert
        Assert.False(execState.IsTerminated);
        Assert.Equal(AuthenticationStatus.Awaiting, authState.FirstFactorStatus);
        Assert.Equal(AuthenticationStatus.Awaiting, authState.SecondFactorStatus);
    }
    
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExecuteAsync_UserNameSuffixNotPermitted_ShouldTerminatePipeline(bool upnRequired)
    {
        //Arrange
        var step = new UserNameValidationStep(NullLogger<UserNameValidationStep>.Instance);
        var serverConfigMock = new Mock<ILdapServerConfiguration>();
        serverConfigMock.Setup(x => x.UpnRequired).Returns(upnRequired);
        serverConfigMock.Setup(x => x.SuffixesPermissions).Returns(new PermissionRules(new List<string>(){"domain2"}, new List<string>()));
        var execState = new ExecutionState();
        var authState = new AuthenticationState();
        var responseInfo = new ResponseInformation();
        var contextMock = new Mock<IRadiusPipelineExecutionContext>();
        contextMock.Setup(x=> x.RequestPacket.UserName).Returns("user@domain.com");
        contextMock.Setup(x=> x.LdapServerConfiguration).Returns(serverConfigMock.Object);
        contextMock.Setup(x => x.ResponseInformation).Returns(responseInfo);
        contextMock.Setup(x => x.AuthenticationState).Returns(authState);
        contextMock.Setup(x => x.ExecutionState).Returns(execState);
        var context = contextMock.Object;

        //Act
        await step.ExecuteAsync(context);
        
        //Assert
        Assert.True(execState.IsTerminated);
        Assert.Equal(AuthenticationStatus.Reject, authState.FirstFactorStatus);
        Assert.Equal(AuthenticationStatus.Reject, authState.SecondFactorStatus);
    }
}