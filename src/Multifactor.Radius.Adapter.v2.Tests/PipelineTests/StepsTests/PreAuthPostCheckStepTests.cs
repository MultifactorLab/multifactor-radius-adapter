using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Multifactor.Core.Ldap.Schema;
using Multifactor.Radius.Adapter.v2.Core.Auth;
using Multifactor.Radius.Adapter.v2.Core.Pipeline;
using Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline.Context;
using Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline.Steps;

namespace Multifactor.Radius.Adapter.v2.Tests.PipelineTests.StepsTests;

public class PreAuthPostCheckStepTests
{
    [Theory]
    [InlineData(AuthenticationStatus.Accept)]
    [InlineData(AuthenticationStatus.Bypass)]
    public async Task SuccessfulSecondFactor_ShouldBypass(AuthenticationStatus status)
    {
        var contextMock = new Mock<IRadiusPipelineExecutionContext>();
        var execState = new ExecutionState();
        contextMock.Setup(x => x.AuthenticationState.SecondFactorStatus).Returns(status);
        contextMock.Setup(x => x.RequestPacket.UserName).Returns("Test");
        contextMock.Setup(x => x.LdapSchema).Returns(LdapSchemaBuilder.Default);
        contextMock.Setup(x => x.ExecutionState).Returns(execState);
        
        var context = contextMock.Object;
        var step = new PreAuthPostCheck(NullLogger<PreAuthPostCheck>.Instance);
        await step.ExecuteAsync(context);
        Assert.False(execState.IsTerminated);
    }
    
    [Theory]
    [InlineData(AuthenticationStatus.Reject)]
    [InlineData(AuthenticationStatus.Awaiting)]
    public async Task UnsuccessfulSecondFactor_ShouldTerminatePipeline(AuthenticationStatus status)
    {
        var contextMock = new Mock<IRadiusPipelineExecutionContext>();
        var execState = new ExecutionState();
        contextMock.Setup(x => x.AuthenticationState.SecondFactorStatus).Returns(status);
        contextMock.Setup(x => x.RequestPacket.UserName).Returns("Test");
        contextMock.Setup(x => x.LdapSchema).Returns(LdapSchemaBuilder.Default);
        contextMock.Setup(x => x.ExecutionState).Returns(execState);
        
        var context =  contextMock.Object;
        var step = new PreAuthPostCheck(NullLogger<PreAuthPostCheck>.Instance);
        await step.ExecuteAsync(context);
        Assert.True(execState.IsTerminated);
    }
}