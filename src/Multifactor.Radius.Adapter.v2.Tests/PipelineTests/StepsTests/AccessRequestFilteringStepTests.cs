using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Multifactor.Radius.Adapter.v2.Core.Auth;
using Multifactor.Radius.Adapter.v2.Core.Pipeline;
using Multifactor.Radius.Adapter.v2.Core.Radius.Packet;
using Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline.Context;
using Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline.Steps;

namespace Multifactor.Radius.Adapter.v2.Tests.PipelineTests.StepsTests;

public class AccessRequestFilteringStepTests
{
    [Fact]
    public async Task Execute_AccessRequestPacket_ShouldExecuteStep()
    {
        var context = GetContextMock(PacketCode.AccessRequest);
        var statusServerFilteringStep = new AccessRequestFilteringStep(NullLogger<AccessRequestFilteringStep>.Instance);
        await statusServerFilteringStep.ExecuteAsync(context);
        
        Assert.False(context.ExecutionState.IsTerminated);
        Assert.False(context.ExecutionState.ShouldSkipResponse);
        Assert.Equal(AuthenticationStatus.Awaiting, context.AuthenticationState.FirstFactorStatus);
        Assert.Equal(AuthenticationStatus.Awaiting, context.AuthenticationState.SecondFactorStatus);
    }
    
    [Fact]
    public async Task Execute_NotAccessRequestPacket_ShouldTerminatePipeline()
    {
        var context = GetContextMock(PacketCode.CoaRequest);
        var statusServerFilteringStep = new AccessRequestFilteringStep(NullLogger<AccessRequestFilteringStep>.Instance);
        await statusServerFilteringStep.ExecuteAsync(context);
        
        Assert.True(context.ExecutionState.IsTerminated);
        Assert.True(context.ExecutionState.ShouldSkipResponse);
        Assert.Equal(AuthenticationStatus.Awaiting, context.AuthenticationState.FirstFactorStatus);
        Assert.Equal(AuthenticationStatus.Awaiting, context.AuthenticationState.SecondFactorStatus);
    }

    private IRadiusPipelineExecutionContext GetContextMock(PacketCode packetCode)
    {
        var packetMock = new Mock<IRadiusPacket>();
        packetMock.Setup(x => x.Code).Returns(packetCode);
        
        var authState = new AuthenticationState();
        var responseInformation = new ResponseInformation();
        var execState = new ExecutionState();
        
        var contextMock = new Mock<IRadiusPipelineExecutionContext>();
        contextMock.Setup(x => x.RequestPacket).Returns(packetMock.Object);
        contextMock.Setup(x => x.AuthenticationState).Returns(authState);
        contextMock.Setup(x => x.ResponseInformation).Returns(responseInformation);
        contextMock.Setup(x => x.ExecutionState).Returns(execState);
        return contextMock.Object;
    }
}