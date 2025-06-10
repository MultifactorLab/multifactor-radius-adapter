using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Multifactor.Radius.Adapter.v2.Core.AccessChallenge;
using Multifactor.Radius.Adapter.v2.Core.Auth;
using Multifactor.Radius.Adapter.v2.Core.Auth.PreAuthMode;
using Multifactor.Radius.Adapter.v2.Core.MultifactorApi;
using Multifactor.Radius.Adapter.v2.Core.Radius;
using Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline.Context;
using Multifactor.Radius.Adapter.v2.Services.MultifactorApi;
using Multifactor.Radius.Adapter.v2.Tests.Fixture;

namespace Multifactor.Radius.Adapter.v2.Tests.AccessChallengeTests;

public class SecondFactorChallengeProcessorTests
{
    [Fact]
    public void ShouldReturnCorrectChallengeType()
    {
        var mfServiceMock = new Mock<IMultifactorApiService>();
        var mfService = mfServiceMock.Object;
        var processor =
            new SecondFactorChallengeProcessor(mfService, NullLogger<SecondFactorChallengeProcessor>.Instance);
        Assert.Equal(ChallengeType.SecondFactor, processor.ChallengeType);
    }

    [Fact]
    public void AddChallengeContext_NoContext_ShouldThrowArgumentNullException()
    {
        var mfServiceMock = new Mock<IMultifactorApiService>();
        var mfService = mfServiceMock.Object;
        var processor =
            new SecondFactorChallengeProcessor(mfService, NullLogger<SecondFactorChallengeProcessor>.Instance);

        Assert.Throws<ArgumentNullException>(() => processor.AddChallengeContext(null));
    }

    [Theory]
    [ClassData(typeof(EmptyStringsListInput))]
    public void AddChallengeContext_NoState_ShouldThrowArgumentException(string emptyString)
    {
        var mfServiceMock = new Mock<IMultifactorApiService>();
        var mfService = mfServiceMock.Object;
        var processor =
            new SecondFactorChallengeProcessor(mfService, NullLogger<SecondFactorChallengeProcessor>.Instance);
        var contextMock = new Mock<IRadiusPipelineExecutionContext>();
        contextMock.Setup(x => x.ResponseInformation.State).Returns(emptyString);
        Assert.ThrowsAny<ArgumentException>(() => processor.AddChallengeContext(contextMock.Object));
    }

    [Fact]
    public void AddChallengeContext_ShouldAdd()
    {
        var mfServiceMock = new Mock<IMultifactorApiService>();
        var mfService = mfServiceMock.Object;
        var processor =
            new SecondFactorChallengeProcessor(mfService, NullLogger<SecondFactorChallengeProcessor>.Instance);
        var contextMock = new Mock<IRadiusPipelineExecutionContext>();
        contextMock.Setup(x => x.Settings.ClientConfigurationName).Returns("config");
        contextMock.Setup(x => x.ResponseInformation.State).Returns("state");
        contextMock.Setup(x => x.RequestPacket.Identifier).Returns(1);

        var id = processor.AddChallengeContext(contextMock.Object);
        Assert.NotNull(id);
        Assert.Equal("state", id.RequestId);
        Assert.True(processor.HasChallengeContext(id));
    }

    [Fact]
    public void AddChallengeContext_SameId_ShouldNotAdd()
    {
        var mfServiceMock = new Mock<IMultifactorApiService>();
        var mfService = mfServiceMock.Object;
        var processor =
            new SecondFactorChallengeProcessor(mfService, NullLogger<SecondFactorChallengeProcessor>.Instance);
        var contextMock = new Mock<IRadiusPipelineExecutionContext>();
        contextMock.Setup(x => x.Settings.ClientConfigurationName).Returns("config");
        contextMock.Setup(x => x.ResponseInformation.State).Returns("state");
        contextMock.Setup(x => x.RequestPacket.Identifier).Returns(1);
        var context = contextMock.Object;

        processor.AddChallengeContext(context);
        var id = processor.AddChallengeContext(context);
        Assert.NotNull(id);
        Assert.Empty(id.RequestId);
    }

    [Theory]
    [ClassData(typeof(EmptyStringsListInput))]
    public async Task ProcessChallenge_EmptyName_ShouldReject(string userName)
    {
        var mfServiceMock = new Mock<IMultifactorApiService>();
        var mfService = mfServiceMock.Object;
        var processor = new SecondFactorChallengeProcessor(mfService, NullLogger<SecondFactorChallengeProcessor>.Instance);
        var contextMock = new Mock<IRadiusPipelineExecutionContext>();
        var endpoint = IPEndPoint.Parse("127.0.0.1:8080");
        contextMock.Setup(x => x.RequestPacket.UserName).Returns(userName);
        contextMock.Setup(x => x.RequestPacket.Identifier).Returns(1);
        contextMock.Setup(x => x.RemoteEndpoint).Returns(endpoint);
        contextMock.SetupProperty(x => x.AuthenticationState.SecondFactorStatus);
        contextMock.SetupProperty(x => x.ResponseInformation.State);
        
        var context = contextMock.Object;
        var id = new ChallengeIdentifier("1", "2");
        var status = await processor.ProcessChallengeAsync(id, context);
        
        Assert.Equal(ChallengeStatus.Reject, status);
        Assert.Equal(AuthenticationStatus.Reject, context.AuthenticationState.SecondFactorStatus);
        Assert.Equal(id.RequestId, context.ResponseInformation.State);
    }

    [Theory]
    [InlineData(AuthenticationType.Unknown)]
    [InlineData(AuthenticationType.EAP)]
    [InlineData(AuthenticationType.CHAP)]
    [InlineData(AuthenticationType.MSCHAP)]
    public async Task ProcessAuthenticationType_UnsupportedType_ShouldReject(AuthenticationType authType)
    {
        var mfServiceMock = new Mock<IMultifactorApiService>();
        var mfService = mfServiceMock.Object;
        var processor = new SecondFactorChallengeProcessor(mfService, NullLogger<SecondFactorChallengeProcessor>.Instance);
        var contextMock = new Mock<IRadiusPipelineExecutionContext>();
        var endpoint = IPEndPoint.Parse("127.0.0.1:8080");
        contextMock.Setup(x => x.RequestPacket.UserName).Returns("userName");
        contextMock.Setup(x => x.RequestPacket.Identifier).Returns(1);
        contextMock.Setup(x => x.RequestPacket.AuthenticationType).Returns(authType);
        contextMock.Setup(x => x.RequestPacket.TryGetUserPassword()).Returns("password");
        contextMock.Setup(x => x.RemoteEndpoint).Returns(endpoint);
        contextMock.Setup(x => x.Settings.PreAuthnMode).Returns(PreAuthModeDescriptor.Default);
        
        contextMock.SetupProperty(x => x.AuthenticationState.SecondFactorStatus);
        contextMock.SetupProperty(x => x.ResponseInformation.State);
        
        var context = contextMock.Object;
        var id = new ChallengeIdentifier("1", "2");
        var status = await processor.ProcessChallengeAsync(id, context);
        
        Assert.Equal(ChallengeStatus.Reject, status);
        Assert.Equal(AuthenticationStatus.Reject, context.AuthenticationState.SecondFactorStatus);
        Assert.Equal(id.RequestId, context.ResponseInformation.State);
    }
    
    [Theory]
    [InlineData(AuthenticationType.PAP)]
    [InlineData(AuthenticationType.MSCHAP2)]
    public async Task ProcessAuthenticationType_SupportedTypeNoContext_ShouldReject(AuthenticationType authType)
    {
        var mfServiceMock = new Mock<IMultifactorApiService>();
        var mfService = mfServiceMock.Object;
        var processor = new SecondFactorChallengeProcessor(mfService, NullLogger<SecondFactorChallengeProcessor>.Instance);
        var contextMock = new Mock<IRadiusPipelineExecutionContext>();
        var endpoint = IPEndPoint.Parse("127.0.0.1:8080");
        contextMock.Setup(x => x.RequestPacket.UserName).Returns("userName");
        contextMock.Setup(x => x.RequestPacket.Identifier).Returns(1);
        contextMock.Setup(x => x.RequestPacket.AuthenticationType).Returns(authType);
        contextMock.Setup(x => x.RequestPacket.TryGetUserPassword()).Returns("password");
        contextMock.Setup(x => x.RemoteEndpoint).Returns(endpoint);
        contextMock.Setup(x => x.Settings.PreAuthnMode).Returns(PreAuthModeDescriptor.Default);
        
        contextMock.SetupProperty(x => x.AuthenticationState.SecondFactorStatus);
        contextMock.SetupProperty(x => x.ResponseInformation.State);
        
        var context = contextMock.Object;
        var id = new ChallengeIdentifier("1", "2");
        await Assert.ThrowsAsync<InvalidOperationException>(() => processor.ProcessChallengeAsync(id, context));
    }
    
    [Fact]
    public async Task ProcessAuthenticationType_PositiveApiResponse_ShouldAccept()
    {
        var mfServiceMock = new Mock<IMultifactorApiService>();
        mfServiceMock
            .Setup(x => x.SendChallengeAsync(It.IsAny<IRadiusPipelineExecutionContext>(), It.IsAny<string>(), It.IsAny<ChallengeIdentifier>()))
            .ReturnsAsync(new MultifactorResponse(AuthenticationStatus.Accept));
        var mfService = mfServiceMock.Object;
        var processor = new SecondFactorChallengeProcessor(mfService, NullLogger<SecondFactorChallengeProcessor>.Instance);
        var contextMock = new Mock<IRadiusPipelineExecutionContext>();
        var endpoint = IPEndPoint.Parse("127.0.0.1:8080");
        contextMock.Setup(x => x.RequestPacket.UserName).Returns("userName");
        contextMock.Setup(x => x.RequestPacket.Identifier).Returns(1);
        contextMock.Setup(x => x.RequestPacket.AuthenticationType).Returns(AuthenticationType.PAP);
        contextMock.Setup(x => x.RequestPacket.TryGetUserPassword()).Returns("password");
        contextMock.Setup(x => x.RemoteEndpoint).Returns(endpoint);
        contextMock.Setup(x => x.Settings.PreAuthnMode).Returns(PreAuthModeDescriptor.Default);
        contextMock.Setup(x => x.Settings.ClientConfigurationName).Returns("1");
        contextMock.SetupProperty(x => x.AuthenticationState.SecondFactorStatus);
        contextMock.SetupProperty(x => x.ResponseInformation.State);
        var context = contextMock.Object;
        
        context.ResponseInformation.State = "2";
        var id = new ChallengeIdentifier(context.Settings.ClientConfigurationName, context.ResponseInformation.State);
        processor.AddChallengeContext(context);
        var result = await processor.ProcessChallengeAsync(id, context);
        Assert.Equal(ChallengeStatus.Accept, result);
        Assert.Equal(AuthenticationStatus.Accept, context.AuthenticationState.SecondFactorStatus);
        Assert.False(processor.HasChallengeContext(id));
    }
    
    [Fact]
    public async Task ProcessAuthenticationType_NegativeApiResponse_ShouldAccept()
    {
        var mfServiceMock = new Mock<IMultifactorApiService>();
        mfServiceMock
            .Setup(x => x.SendChallengeAsync(It.IsAny<IRadiusPipelineExecutionContext>(), It.IsAny<string>(), It.IsAny<ChallengeIdentifier>()))
            .ReturnsAsync(new MultifactorResponse(AuthenticationStatus.Reject));
        var mfService = mfServiceMock.Object;
        var processor = new SecondFactorChallengeProcessor(mfService, NullLogger<SecondFactorChallengeProcessor>.Instance);
        var contextMock = new Mock<IRadiusPipelineExecutionContext>();
        var endpoint = IPEndPoint.Parse("127.0.0.1:8080");
        contextMock.Setup(x => x.RequestPacket.UserName).Returns("userName");
        contextMock.Setup(x => x.RequestPacket.Identifier).Returns(1);
        contextMock.Setup(x => x.RequestPacket.AuthenticationType).Returns(AuthenticationType.PAP);
        contextMock.Setup(x => x.RequestPacket.TryGetUserPassword()).Returns("password");
        contextMock.Setup(x => x.RemoteEndpoint).Returns(endpoint);
        contextMock.Setup(x => x.Settings.PreAuthnMode).Returns(PreAuthModeDescriptor.Default);
        contextMock.Setup(x => x.Settings.ClientConfigurationName).Returns("1");
        contextMock.SetupProperty(x => x.AuthenticationState.SecondFactorStatus);
        contextMock.SetupProperty(x => x.ResponseInformation.State);
        var context = contextMock.Object;
        
        context.ResponseInformation.State = "2";
        var id = new ChallengeIdentifier(context.Settings.ClientConfigurationName, context.ResponseInformation.State);
        processor.AddChallengeContext(context);
        var result = await processor.ProcessChallengeAsync(id, context);
        Assert.Equal(ChallengeStatus.Reject, result);
        Assert.Equal(AuthenticationStatus.Reject, context.AuthenticationState.SecondFactorStatus);
        Assert.False(processor.HasChallengeContext(id));
    }
    
    [Theory]
    [InlineData(AuthenticationStatus.Awaiting)]
    [InlineData(AuthenticationStatus.Bypass)]
    public async Task ProcessAuthenticationType_NeutralApiResponse_ShouldAccept(AuthenticationStatus status)
    {
        var mfServiceMock = new Mock<IMultifactorApiService>();
        mfServiceMock
            .Setup(x => x.SendChallengeAsync(It.IsAny<IRadiusPipelineExecutionContext>(), It.IsAny<string>(), It.IsAny<ChallengeIdentifier>()))
            .ReturnsAsync(new MultifactorResponse(status));
        var mfService = mfServiceMock.Object;
        var processor = new SecondFactorChallengeProcessor(mfService, NullLogger<SecondFactorChallengeProcessor>.Instance);
        var contextMock = new Mock<IRadiusPipelineExecutionContext>();
        var endpoint = IPEndPoint.Parse("127.0.0.1:8080");
        contextMock.Setup(x => x.RequestPacket.UserName).Returns("userName");
        contextMock.Setup(x => x.RequestPacket.Identifier).Returns(1);
        contextMock.Setup(x => x.RequestPacket.AuthenticationType).Returns(AuthenticationType.PAP);
        contextMock.Setup(x => x.RequestPacket.TryGetUserPassword()).Returns("password");
        contextMock.Setup(x => x.RemoteEndpoint).Returns(endpoint);
        contextMock.Setup(x => x.Settings.PreAuthnMode).Returns(PreAuthModeDescriptor.Default);
        contextMock.Setup(x => x.Settings.ClientConfigurationName).Returns("1");
        contextMock.SetupProperty(x => x.AuthenticationState.SecondFactorStatus);
        contextMock.SetupProperty(x => x.ResponseInformation.State);
        var context = contextMock.Object;
        
        context.ResponseInformation.State = "2";
        var id = new ChallengeIdentifier(context.Settings.ClientConfigurationName, context.ResponseInformation.State);
        processor.AddChallengeContext(context);
        var result = await processor.ProcessChallengeAsync(id, context);
        Assert.Equal(ChallengeStatus.InProcess, result);
    }
}