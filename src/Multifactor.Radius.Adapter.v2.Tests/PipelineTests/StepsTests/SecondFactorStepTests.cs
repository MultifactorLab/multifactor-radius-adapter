using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Multifactor.Radius.Adapter.v2.Core.AccessChallenge;
using Multifactor.Radius.Adapter.v2.Core.Auth;
using Multifactor.Radius.Adapter.v2.Core.Configuration.Client;
using Multifactor.Radius.Adapter.v2.Core.MultifactorApi;
using Multifactor.Radius.Adapter.v2.Core.Pipeline;
using Multifactor.Radius.Adapter.v2.Core.Radius.Packet;
using Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline.Context;
using Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline.Steps;
using Multifactor.Radius.Adapter.v2.Services.Ldap;
using Multifactor.Radius.Adapter.v2.Services.MultifactorApi;

namespace Multifactor.Radius.Adapter.v2.Tests.PipelineTests.StepsTests;

public class SecondFactorStepTests
{
    [Fact]
    public async Task ExecuteAsync_AclRequest_ShouldSetBypass()
    {
        var apiServiceMock = new Mock<IMultifactorApiService>();
        var challengeProviderMock = new Mock<IChallengeProcessorProvider>();
        var groupServiceMock = new Mock<ILdapGroupService>();
        
        var step = new SecondFactorStep(apiServiceMock.Object, challengeProviderMock.Object, groupServiceMock.Object, NullLogger<SecondFactorStep>.Instance);

        var packetMock = new Mock<IRadiusPacket>();
        packetMock.Setup(x => x.IsVendorAclRequest).Returns(true);
        var contextMock = new Mock<IRadiusPipelineExecutionContext>();
        contextMock.SetupProperty(x => x.AuthenticationState);
        contextMock.Setup(x => x.RequestPacket).Returns(packetMock.Object);
        contextMock.Setup(x => x.Settings.FirstFactorAuthenticationSource).Returns(AuthenticationSource.Radius);
        contextMock.Setup(x => x.RequestPacket.UserName).Returns("username");
        contextMock.Setup(x => x.RemoteEndpoint).Returns(IPEndPoint.Parse("127.0.0.1:1"));
        
        var ldapConfig = new Mock<ILdapServerConfiguration>();
        ldapConfig.Setup(x => x.SecondFaGroups).Returns(new List<string>());
        ldapConfig.Setup(x => x.SecondFaBypassGroups).Returns(new List<string>());
        contextMock.Setup(x => x.FirstFactorLdapServerConfiguration).Returns(ldapConfig.Object); 
        
        var context = contextMock.Object;
        context.AuthenticationState = new AuthenticationState();
        await step.ExecuteAsync(context);
        
        Assert.Equal(AuthenticationStatus.Bypass, context.AuthenticationState.SecondFactorStatus);
    }
    
    [Theory]
    [InlineData(AuthenticationStatus.Bypass)]
    [InlineData(AuthenticationStatus.Reject)]
    [InlineData(AuthenticationStatus.Accept)]
    [InlineData(AuthenticationStatus.Awaiting)]
    public async Task ExecuteAsync_ShouldSecondFactorStatus(AuthenticationStatus apiResponseStatus)
    {
        var apiServiceMock = new Mock<IMultifactorApiService>();
        apiServiceMock.Setup(x => x.CreateSecondFactorRequestAsync(It.IsAny<IRadiusPipelineExecutionContext>())).ReturnsAsync(new MultifactorResponse(apiResponseStatus, "state", "message"));
        
        var challengeProviderMock = new Mock<IChallengeProcessorProvider>();
        challengeProviderMock.Setup(x => x.GetChallengeProcessorByType(ChallengeType.SecondFactor)).Returns(new Mock<IChallengeProcessor>().Object);
        
        var groupServiceMock = new Mock<ILdapGroupService>();
        
        var step = new SecondFactorStep(apiServiceMock.Object, challengeProviderMock.Object, groupServiceMock.Object, NullLogger<SecondFactorStep>.Instance);

        var packetMock = new Mock<IRadiusPacket>();
        packetMock.Setup(x => x.IsVendorAclRequest).Returns(false);
        var contextMock = new Mock<IRadiusPipelineExecutionContext>();
        contextMock.SetupProperty(x => x.AuthenticationState);
        contextMock.Setup(x => x.RequestPacket).Returns(packetMock.Object);
        contextMock.Setup(x => x.Settings.FirstFactorAuthenticationSource).Returns(AuthenticationSource.Radius);
        contextMock.Setup(x => x.RequestPacket.UserName).Returns("username");
        contextMock.Setup(x => x.RemoteEndpoint).Returns(IPEndPoint.Parse("127.0.0.1:1"));
        contextMock.SetupProperty(x => x.ResponseInformation);
        
        var ldapConfig = new Mock<ILdapServerConfiguration>();
        ldapConfig.Setup(x => x.SecondFaGroups).Returns(new List<string>());
        ldapConfig.Setup(x => x.SecondFaBypassGroups).Returns(new List<string>());
        contextMock.Setup(x => x.FirstFactorLdapServerConfiguration).Returns(ldapConfig.Object); 
        
        var context = contextMock.Object;
        context.AuthenticationState = new AuthenticationState();
        context.ResponseInformation = new ResponseInformation();
        await step.ExecuteAsync(context);
        
        Assert.Equal(apiResponseStatus, context.AuthenticationState.SecondFactorStatus);
        Assert.Equal("state", context.ResponseInformation.State);
        Assert.Equal("message", context.ResponseInformation.ReplyMessage);
    }
    
    [Fact]
    public async Task ExecuteAsync_AwaitingResponse_ShouldAddChallengeContext()
    {
        var apiServiceMock = new Mock<IMultifactorApiService>();
        apiServiceMock
            .Setup(x => x.CreateSecondFactorRequestAsync(It.IsAny<IRadiusPipelineExecutionContext>()))
            .ReturnsAsync(new MultifactorResponse(AuthenticationStatus.Awaiting, "state", "message"));
        
        var challengeProviderMock = new Mock<IChallengeProcessorProvider>();
        var processorMock = new Mock<IChallengeProcessor>();
        challengeProviderMock.Setup(x => x.GetChallengeProcessorByType(ChallengeType.SecondFactor)).Returns(processorMock.Object);
        
        var groupServiceMock = new Mock<ILdapGroupService>();
        
        var step = new SecondFactorStep(apiServiceMock.Object, challengeProviderMock.Object, groupServiceMock.Object, NullLogger<SecondFactorStep>.Instance);

        var packetMock = new Mock<IRadiusPacket>();
        packetMock.Setup(x => x.IsVendorAclRequest).Returns(false);
        var contextMock = new Mock<IRadiusPipelineExecutionContext>();
        contextMock.SetupProperty(x => x.AuthenticationState);
        contextMock.Setup(x => x.RequestPacket).Returns(packetMock.Object);
        contextMock.Setup(x => x.Settings.FirstFactorAuthenticationSource).Returns(AuthenticationSource.Radius);
        contextMock.Setup(x => x.RequestPacket.UserName).Returns("username");
        contextMock.Setup(x => x.RemoteEndpoint).Returns(IPEndPoint.Parse("127.0.0.1:1"));
        contextMock.SetupProperty(x => x.ResponseInformation);
        
        var ldapConfig = new Mock<ILdapServerConfiguration>();
        ldapConfig.Setup(x => x.SecondFaGroups).Returns(new List<string>());
        ldapConfig.Setup(x => x.SecondFaBypassGroups).Returns(new List<string>());
        contextMock.Setup(x => x.FirstFactorLdapServerConfiguration).Returns(ldapConfig.Object); 
        
        var context = contextMock.Object;
        context.AuthenticationState = new AuthenticationState();
        context.ResponseInformation = new ResponseInformation();
        await step.ExecuteAsync(context);
        
        Assert.Equal(AuthenticationStatus.Awaiting, context.AuthenticationState.SecondFactorStatus);
        Assert.Equal("state", context.ResponseInformation.State);
        Assert.Equal("message", context.ResponseInformation.ReplyMessage);
        processorMock.Verify(x => x.AddChallengeContext(context), Times.Once);
    }
}