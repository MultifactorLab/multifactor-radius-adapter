using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Multifactor.Radius.Adapter.v2.Core.Auth;
using Multifactor.Radius.Adapter.v2.Core.Pipeline;
using Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline.Context;
using Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline.Steps;
using NetTools;

namespace Multifactor.Radius.Adapter.v2.Tests.Unit.PipelineSteps;

public class IpWhiteListStepTests
{
    [Fact]
    public async Task EmptyWhiteList_ShouldNotTerminatePipeline()
    {
        var step = new IpWhiteListStep(NullLogger<IpWhiteListStep>.Instance);

        var contextMock = new Mock<IRadiusPipelineExecutionContext>();
        var authState = new AuthenticationState();
        contextMock.Setup(x => x.AuthenticationState).Returns(authState);
        var executionState = new ExecutionState();
        contextMock.Setup(x => x.ExecutionState).Returns(executionState);
        contextMock.Setup(x => x.IpWhiteList).Returns([]);
        var context = contextMock.Object;
        await step.ExecuteAsync(context);
        
        Assert.Equal(AuthenticationStatus.Awaiting, authState.FirstFactorStatus);
        Assert.Equal(AuthenticationStatus.Awaiting, authState.SecondFactorStatus);
        Assert.False(executionState.IsTerminated);
    }

    [Fact]
    public async Task ClientIpInRange_ShouldNotTerminatePipeline()
    {
        var step = new IpWhiteListStep(NullLogger<IpWhiteListStep>.Instance);

        var contextMock = new Mock<IRadiusPipelineExecutionContext>();
        var authState = new AuthenticationState();
        contextMock.Setup(x => x.AuthenticationState).Returns(authState);
        var executionState = new ExecutionState();
        contextMock.Setup(x => x.ExecutionState).Returns(executionState);
        contextMock.Setup(x => x.IpWhiteList).Returns([IPAddressRange.Parse("127.0.0.1")]);
        contextMock.Setup(x => x.RemoteEndpoint).Returns(IPEndPoint.Parse("127.0.0.1"));
        contextMock.Setup(x => x.RequestPacket.CallingStationIdAttribute).Returns(string.Empty);
        var context = contextMock.Object;
        await step.ExecuteAsync(context);
        
        Assert.Equal(AuthenticationStatus.Awaiting, authState.FirstFactorStatus);
        Assert.Equal(AuthenticationStatus.Awaiting, authState.SecondFactorStatus);
        Assert.False(executionState.IsTerminated);
    }

    [Fact]
    public async Task ClientIpNotInRange_ShouldTerminate()
    {
        var step = new IpWhiteListStep(NullLogger<IpWhiteListStep>.Instance);

        var contextMock = new Mock<IRadiusPipelineExecutionContext>();
        var authState = new AuthenticationState();
        contextMock.Setup(x => x.AuthenticationState).Returns(authState);
        var executionState = new ExecutionState();
        contextMock.Setup(x => x.ExecutionState).Returns(executionState);
        contextMock.Setup(x => x.IpWhiteList).Returns([IPAddressRange.Parse("127.0.0.1")]);
        contextMock.Setup(x => x.RemoteEndpoint).Returns(IPEndPoint.Parse("127.0.0.2"));
        contextMock.Setup(x => x.RequestPacket.CallingStationIdAttribute).Returns(string.Empty);
        var context = contextMock.Object;
        await step.ExecuteAsync(context);
        
        Assert.Equal(AuthenticationStatus.Reject, authState.FirstFactorStatus);
        Assert.Equal(AuthenticationStatus.Reject, authState.SecondFactorStatus);
        Assert.True(executionState.IsTerminated);
    }
    
    [Fact]
    public async Task CallingStationIdInRange_ShouldNotTerminatePipeline()
    {
        var step = new IpWhiteListStep(NullLogger<IpWhiteListStep>.Instance);

        var contextMock = new Mock<IRadiusPipelineExecutionContext>();
        var authState = new AuthenticationState();
        contextMock.Setup(x => x.AuthenticationState).Returns(authState);
        var executionState = new ExecutionState();
        contextMock.Setup(x => x.ExecutionState).Returns(executionState);
        contextMock.Setup(x => x.IpWhiteList).Returns([IPAddressRange.Parse("127.0.0.1")]);
        contextMock.Setup(x => x.RemoteEndpoint).Returns(IPEndPoint.Parse("127.0.0.2"));
        contextMock.Setup(x => x.RequestPacket.CallingStationIdAttribute).Returns("127.0.0.1");
        var context = contextMock.Object;
        await step.ExecuteAsync(context);
        
        Assert.Equal(AuthenticationStatus.Awaiting, authState.FirstFactorStatus);
        Assert.Equal(AuthenticationStatus.Awaiting, authState.SecondFactorStatus);
        Assert.False(executionState.IsTerminated);
    }

    [Fact]
    public async Task CallingStationIdNotInRange_ShouldTerminate()
    {
        var step = new IpWhiteListStep(NullLogger<IpWhiteListStep>.Instance);

        var contextMock = new Mock<IRadiusPipelineExecutionContext>();
        var authState = new AuthenticationState();
        contextMock.Setup(x => x.AuthenticationState).Returns(authState);
        var executionState = new ExecutionState();
        contextMock.Setup(x => x.ExecutionState).Returns(executionState);
        contextMock.Setup(x => x.IpWhiteList).Returns([IPAddressRange.Parse("127.0.0.1")]);
        contextMock.Setup(x => x.RemoteEndpoint).Returns(IPEndPoint.Parse("127.0.0.1"));
        contextMock.Setup(x => x.RequestPacket.CallingStationIdAttribute).Returns("127.0.0.2");
        var context = contextMock.Object;
        await step.ExecuteAsync(context);
        
        Assert.Equal(AuthenticationStatus.Reject, authState.FirstFactorStatus);
        Assert.Equal(AuthenticationStatus.Reject, authState.SecondFactorStatus);
        Assert.True(executionState.IsTerminated);
    }
}