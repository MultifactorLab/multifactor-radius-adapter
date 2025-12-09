using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Multifactor.Radius.Adapter.v2.Application.Pipeline.Steps;
using Multifactor.Radius.Adapter.v2.Core;
using Multifactor.Radius.Adapter.v2.Domain;
using Multifactor.Radius.Adapter.v2.Domain.Auth;
using Multifactor.Radius.Adapter.v2.Domain.Pipeline;

namespace Multifactor.Radius.Adapter.v2.Tests.PipelineTests.StepsTests;

public class PreAuthCheckStepTests
{
    [Fact]
    public async Task OptModeWithoutOpt_ShouldTerminatePipeline()
    {
        var contextMock = new Mock<IRadiusPipelineExecutionContext>();
        var preAuth = PreAuthModeDescriptor.Create("otp", new PreAuthModeSettings(10));
        var execState = new ExecutionState();
        contextMock.Setup(x => x.PreAuthnMode).Returns(preAuth);
        contextMock.Setup(x => x.Passphrase).Returns(UserPassphrase.Parse("123456", preAuth));
        contextMock.Setup(x => x.RequestPacket.UserName).Returns("username");
        contextMock.Setup(x => x.RemoteEndpoint).Returns(IPEndPoint.Parse("127.0.0.1:1"));
        contextMock.SetupProperty(x => x.AuthenticationState);
        contextMock.Setup(x => x.ExecutionState).Returns(execState);
        var step = new PreAuthCheckStep(NullLogger<PreAuthCheckStep>.Instance);
        var context = contextMock.Object;
        context.AuthenticationState = new AuthenticationState();
        
        await step.ExecuteAsync(context);

        Assert.Equal(AuthenticationStatus.Reject, context.AuthenticationState.SecondFactorStatus);
        Assert.True(execState.IsTerminated);
    }
    
    [Theory]
    [InlineData("None")]
    [InlineData("Otp")]
    [InlineData("Any")]
    public async Task CorrectPreAuthState_ShouldBypass(string mode)
    {
        var contextMock = new Mock<IRadiusPipelineExecutionContext>();
        var preAuth = PreAuthModeDescriptor.Create(mode, new PreAuthModeSettings(1));
        var execState = new ExecutionState();
        contextMock.Setup(x => x.PreAuthnMode).Returns(preAuth);
        contextMock.Setup(x => x.Passphrase).Returns(UserPassphrase.Parse("123456", preAuth));
        contextMock.Setup(x => x.RequestPacket.UserName).Returns("username");
        contextMock.Setup(x => x.RemoteEndpoint).Returns(IPEndPoint.Parse("127.0.0.1:1"));
        contextMock.SetupProperty(x => x.AuthenticationState);
        contextMock.Setup(x => x.ExecutionState).Returns(execState);
        var context = contextMock.Object;
        context.AuthenticationState = new AuthenticationState();
        
        var step = new PreAuthCheckStep(NullLogger<PreAuthCheckStep>.Instance);
        await step.ExecuteAsync(context);

        Assert.Equal(AuthenticationStatus.Awaiting, context.AuthenticationState.SecondFactorStatus);
        Assert.False(execState.IsTerminated);
    }
}