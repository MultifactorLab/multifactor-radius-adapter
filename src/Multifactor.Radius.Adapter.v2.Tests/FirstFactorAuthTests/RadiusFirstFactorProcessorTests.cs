using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Multifactor.Radius.Adapter.v2.Core.Auth;
using Multifactor.Radius.Adapter.v2.Core.Auth.PreAuthMode;
using Multifactor.Radius.Adapter.v2.Core.FirstFactor;
using Multifactor.Radius.Adapter.v2.Core.Radius;
using Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline.Context;
using Multifactor.Radius.Adapter.v2.Services.Radius;
using Multifactor.Radius.Adapter.v2.Tests.Fixture;

namespace Multifactor.Radius.Adapter.v2.Tests.FirstFactorAuthTests;

[Collection("ActiveDirectory")]
public class RadiusFirstFactorProcessorTests
{
    [Fact]
    public async Task ProcessFirstFactor_ShouldAccept()
    {
        var sensitiveData = GetConfig();
        
        var dictionary = TestUtils.GetRadiusDictionary();
        var packetService = new RadiusPacketService(NullLogger<RadiusPacketService>.Instance, dictionary);
        var secret = new SharedSecret(sensitiveData["Secret"]);
        var processor = new RadiusFirstFactorProcessor(packetService, NullLogger<RadiusFirstFactorProcessor>.Instance);
        
        var contextMock = new Mock<IRadiusPipelineExecutionContext>();
        var authState = new AuthenticationState();
        var transformRules = new UserNameTransformRules();
        var packetBytes = PacketExamples.DefaultAccessRequest;
        var packet = packetService.Parse(packetBytes, secret);
        
        packet.ReplaceAttribute("User-Name", sensitiveData["UserName"]);
        packet.ReplaceAttribute("User-Password", sensitiveData["Password"]);
        
        contextMock.Setup(x => x.RequestPacket).Returns(packet);
        contextMock.Setup(x => x.AuthenticationState).Returns(authState);
        contextMock.Setup(x => x.UserNameTransformRules).Returns(transformRules);
        contextMock.Setup(x => x.NpsServerEndpoint).Returns(IPEndPoint.Parse(sensitiveData["NpsServerEndpoint"]));
        contextMock.Setup(x => x.ServiceClientEndpoint).Returns(IPEndPoint.Parse(sensitiveData["ServiceClientEndpoint"]));
        contextMock.Setup(x => x.PreAuthnMode).Returns(PreAuthModeDescriptor.Default);
        contextMock.Setup(x => x.RadiusSharedSecret).Returns(secret);
        await processor.ProcessFirstFactor(contextMock.Object);
        
        Assert.Equal(AuthenticationStatus.Accept, authState.FirstFactorStatus);
    }
    
    [Fact]
    public async Task ProcessFirstFactor_InvalidPassword_ShouldReject()
    {
        var sensitiveData = GetConfig();
        var dictionary = TestUtils.GetRadiusDictionary();
        var packetService = new RadiusPacketService(NullLogger<RadiusPacketService>.Instance, dictionary);
        var secret = new SharedSecret(sensitiveData["Secret"]);
        var processor = new RadiusFirstFactorProcessor(packetService, NullLogger<RadiusFirstFactorProcessor>.Instance);
        
        var contextMock = new Mock<IRadiusPipelineExecutionContext>();
        var authState = new AuthenticationState();
        var transformRules = new UserNameTransformRules();
        var packetBytes = PacketExamples.DefaultAccessRequest;
        var packet = packetService.Parse(packetBytes, secret);
        
        packet.ReplaceAttribute("User-Name", sensitiveData["UserName"]);
        packet.ReplaceAttribute("User-Password", "pwd");
        
        contextMock.Setup(x => x.RequestPacket).Returns(packet);
        contextMock.Setup(x => x.AuthenticationState).Returns(authState);
        contextMock.Setup(x => x.UserNameTransformRules).Returns(transformRules);
        contextMock.Setup(x => x.NpsServerEndpoint).Returns(IPEndPoint.Parse(sensitiveData["NpsServerEndpoint"]));
        contextMock.Setup(x => x.ServiceClientEndpoint).Returns(IPEndPoint.Parse(sensitiveData["ServiceClientEndpoint"]));
        contextMock.Setup(x => x.PreAuthnMode).Returns(PreAuthModeDescriptor.Default);
        contextMock.Setup(x => x.RadiusSharedSecret).Returns(secret);
        await processor.ProcessFirstFactor(contextMock.Object);
        
        Assert.Equal(AuthenticationStatus.Reject, authState.FirstFactorStatus);
    }
    
    [Fact]
    public async Task ProcessFirstFactor_InvalidLogin_ShouldReject()
    {
        var sensitiveData = GetConfig();
        
        var dictionary = TestUtils.GetRadiusDictionary();
        var packetService = new RadiusPacketService(NullLogger<RadiusPacketService>.Instance, dictionary);
        var secret = new SharedSecret(sensitiveData["Secret"]);
        var processor = new RadiusFirstFactorProcessor(packetService, NullLogger<RadiusFirstFactorProcessor>.Instance);
        
        var contextMock = new Mock<IRadiusPipelineExecutionContext>();
        var authState = new AuthenticationState();
        var transformRules = new UserNameTransformRules();
        var packetBytes = PacketExamples.DefaultAccessRequest;
        var packet = packetService.Parse(packetBytes, secret);
        
        packet.ReplaceAttribute("User-Name", "user");
        packet.ReplaceAttribute("User-Password", sensitiveData["Password"]);
        
        contextMock.Setup(x => x.RequestPacket).Returns(packet);
        contextMock.Setup(x => x.AuthenticationState).Returns(authState);
        contextMock.Setup(x => x.UserNameTransformRules).Returns(transformRules);
        contextMock.Setup(x => x.NpsServerEndpoint).Returns(IPEndPoint.Parse(sensitiveData["NpsServerEndpoint"]));
        contextMock.Setup(x => x.ServiceClientEndpoint).Returns(IPEndPoint.Parse(sensitiveData["ServiceClientEndpoint"]));
        contextMock.Setup(x => x.PreAuthnMode).Returns(PreAuthModeDescriptor.Default);
        contextMock.Setup(x => x.RadiusSharedSecret).Returns(secret);
        await processor.ProcessFirstFactor(contextMock.Object);
        
        Assert.Equal(AuthenticationStatus.Reject, authState.FirstFactorStatus);
    }
    
    private Dictionary<string, string> GetConfig()
    {
        return ConfigUtils.GetConfigSensitiveData("RadiusFirstFactorProcessorTests.txt", "|");
    }
}