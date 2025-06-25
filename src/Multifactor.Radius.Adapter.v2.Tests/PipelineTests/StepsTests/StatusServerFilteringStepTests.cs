using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Multifactor.Radius.Adapter.v2.Core;
using Multifactor.Radius.Adapter.v2.Core.Auth;
using Multifactor.Radius.Adapter.v2.Core.Pipeline;
using Multifactor.Radius.Adapter.v2.Core.Radius.Packet;
using Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline.Context;
using Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline.Steps;

namespace Multifactor.Radius.Adapter.v2.Tests.PipelineTests.StepsTests;

public class StatusServerFilteringStepTests
{
    [Fact]
    public async Task Execute_StatusServerPacket_ShouldExecuteStep()
    {
        var context = GetContextMock(PacketCode.StatusServer);
        var statusServerFilteringStep = new StatusServerFilteringStep(new ApplicationVariables(), NullLogger<StatusServerFilteringStep>.Instance);
        await statusServerFilteringStep.ExecuteAsync(context);
        
        Assert.StartsWith("Server up", context.ResponseInformation.ReplyMessage);
        Assert.True(context.ExecutionState.IsTerminated);
        Assert.Equal(AuthenticationStatus.Accept, context.AuthenticationState.FirstFactorStatus);
        Assert.Equal(AuthenticationStatus.Accept, context.AuthenticationState.SecondFactorStatus);
    }
    
    [Fact]
    public async Task Execute_NotStatusServer_ShouldSkipStep()
    {
        var context = GetContextMock(PacketCode.CoaAck);
        var statusServerFilteringStep = new StatusServerFilteringStep(new ApplicationVariables(), NullLogger<StatusServerFilteringStep>.Instance);
        await statusServerFilteringStep.ExecuteAsync(context);
        
        Assert.Null(context.ResponseInformation.ReplyMessage);
        Assert.False(context.ExecutionState.IsTerminated);
        Assert.Equal(AuthenticationStatus.Awaiting, context.AuthenticationState.FirstFactorStatus);
        Assert.Equal(AuthenticationStatus.Awaiting, context.AuthenticationState.SecondFactorStatus);
    }

    private IRadiusPipelineExecutionContext GetContextMock(PacketCode packetCode)
    {
        var authState = new AuthenticationState();
        var responseInformation = new ResponseInformation();
        var execState = new ExecutionState();
        var packetMock = new Mock<IRadiusPacket>();
        packetMock.Setup(x => x.Code).Returns(packetCode);
        
        var contextMock = new Mock<IRadiusPipelineExecutionContext>();
        contextMock.Setup(x => x.RequestPacket).Returns(packetMock.Object);
        contextMock.Setup(x => x.AuthenticationState).Returns(authState);
        contextMock.Setup(x => x.ResponseInformation).Returns(responseInformation);
        contextMock.Setup(x => x.ExecutionState).Returns(execState);
        
        return contextMock.Object;
    }
}