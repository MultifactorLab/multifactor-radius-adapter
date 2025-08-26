using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Multifactor.Radius.Adapter.v2.Core;
using Multifactor.Radius.Adapter.v2.Core.Auth;
using Multifactor.Radius.Adapter.v2.Core.Auth.PreAuthMode;
using Multifactor.Radius.Adapter.v2.Core.FirstFactor;
using Multifactor.Radius.Adapter.v2.Core.Radius;
using Multifactor.Radius.Adapter.v2.Core.Radius.Packet;
using Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline.Context;
using Multifactor.Radius.Adapter.v2.Services.Radius;

namespace Multifactor.Radius.Adapter.v2.Tests.Unit.FirstFactorAuthTests;

public class RadiusFirstFactorProcessorTests
{
    [Fact]
    public async Task ProcessFirstFactor_ShouldAccept()
    {
        //Arrange
        var clientFactoryMock = new Mock<IRadiusClientFactory>();
        var clientMock = new Mock<IRadiusClient>();
        clientMock.Setup(x => x.SendPacketAsync(It.IsAny<byte>(), It.IsAny<byte[]>(), It.IsAny<IPEndPoint>(), It.IsAny<TimeSpan>())).ReturnsAsync([]);
        clientFactoryMock.Setup(x => x.CreateRadiusClient(It.IsAny<IPEndPoint>())).Returns(clientMock.Object);
        
        var packetService = new Mock<IRadiusPacketService>();
        packetService.Setup(x => x.GetBytes(It.IsAny<IRadiusPacket>(),It.IsAny<SharedSecret>())).Returns([]);

        var responseMock = new Mock<IRadiusPacket>();
        responseMock.Setup(x => x.Code).Returns(PacketCode.AccessAccept);
        
        packetService.Setup(x => x.Parse(It.IsAny<byte[]>(), It.IsAny<SharedSecret>(), It.IsAny<RadiusAuthenticator>())).Returns(responseMock.Object);
        var processor = new RadiusFirstFactorProcessor(packetService.Object, clientFactoryMock.Object, NullLogger<RadiusFirstFactorProcessor>.Instance);
        
        var contextMock = new Mock<IRadiusPipelineExecutionContext>();
        var authState = new AuthenticationState();
        var transformRules = new UserNameTransformRules();
        var requestPacketMock = new Mock<IRadiusPacket>();
        requestPacketMock.Setup(x => x.UserName).Returns("username");
        requestPacketMock.Setup(x => x.Code).Returns(PacketCode.AccessRequest);
        requestPacketMock.Setup(x => x.Identifier).Returns(1);
        requestPacketMock.Setup(x => x.Authenticator).Returns(new RadiusAuthenticator());
        requestPacketMock.Setup(x => x.Attributes).Returns(new Dictionary<string, RadiusAttribute>());
        
        contextMock.Setup(x => x.RequestPacket).Returns(requestPacketMock.Object);
        contextMock.Setup(x => x.AuthenticationState).Returns(authState);
        contextMock.Setup(x => x.UserNameTransformRules).Returns(transformRules);
        contextMock.Setup(x => x.NpsServerEndpoints).Returns(new HashSet<IPEndPoint>([IPEndPoint.Parse("127.0.0.1")]));
        contextMock.Setup(x => x.ServiceClientEndpoint).Returns(IPEndPoint.Parse("127.0.0.1"));
        contextMock.Setup(x => x.PreAuthnMode).Returns(PreAuthModeDescriptor.Default);
        contextMock.Setup(x => x.RadiusSharedSecret).Returns(new SharedSecret("123"));
        contextMock.Setup(x => x.Passphrase).Returns(UserPassphrase.Parse("123", PreAuthModeDescriptor.Default));
        
        //Act
        await processor.ProcessFirstFactor(contextMock.Object);
        
        //Assert
        Assert.Equal(AuthenticationStatus.Accept, authState.FirstFactorStatus);
    }
    
    [Theory]
    [InlineData(PacketCode.StatusServer)]
    [InlineData(PacketCode.AccessReject)]
    [InlineData(PacketCode.AccessChallenge)]
    public async Task ProcessFirstFactor_NoneAcceptCode_ShouldReturnReject(PacketCode responseCode)
    {
        //Arrange
        var clientFactoryMock = new Mock<IRadiusClientFactory>();
        var clientMock = new Mock<IRadiusClient>();
        clientMock.Setup(x => x.SendPacketAsync(It.IsAny<byte>(), It.IsAny<byte[]>(), It.IsAny<IPEndPoint>(), It.IsAny<TimeSpan>())).ReturnsAsync([]);
        clientFactoryMock.Setup(x => x.CreateRadiusClient(It.IsAny<IPEndPoint>())).Returns(clientMock.Object);
        
        var packetService = new Mock<IRadiusPacketService>();
        packetService.Setup(x => x.GetBytes(It.IsAny<IRadiusPacket>(),It.IsAny<SharedSecret>())).Returns([]);

        var responseMock = new Mock<IRadiusPacket>();
        responseMock.Setup(x => x.Code).Returns(responseCode);
        
        packetService.Setup(x => x.Parse(It.IsAny<byte[]>(), It.IsAny<SharedSecret>(), It.IsAny<RadiusAuthenticator>())).Returns(responseMock.Object);
        var processor = new RadiusFirstFactorProcessor(packetService.Object, clientFactoryMock.Object, NullLogger<RadiusFirstFactorProcessor>.Instance);
        
        var contextMock = new Mock<IRadiusPipelineExecutionContext>();
        var authState = new AuthenticationState();
        var transformRules = new UserNameTransformRules();
        var requestPacketMock = new Mock<IRadiusPacket>();
        requestPacketMock.Setup(x => x.UserName).Returns("username");
        requestPacketMock.Setup(x => x.Code).Returns(PacketCode.AccessRequest);
        requestPacketMock.Setup(x => x.Identifier).Returns(1);
        requestPacketMock.Setup(x => x.Authenticator).Returns(new RadiusAuthenticator());
        requestPacketMock.Setup(x => x.Attributes).Returns(new Dictionary<string, RadiusAttribute>());
        
        contextMock.Setup(x => x.RequestPacket).Returns(requestPacketMock.Object);
        contextMock.Setup(x => x.AuthenticationState).Returns(authState);
        contextMock.Setup(x => x.UserNameTransformRules).Returns(transformRules);
        contextMock.Setup(x => x.NpsServerEndpoints).Returns(new HashSet<IPEndPoint>([IPEndPoint.Parse("127.0.0.1")]));
        contextMock.Setup(x => x.ServiceClientEndpoint).Returns(IPEndPoint.Parse("127.0.0.1"));
        contextMock.Setup(x => x.PreAuthnMode).Returns(PreAuthModeDescriptor.Default);
        contextMock.Setup(x => x.RadiusSharedSecret).Returns(new SharedSecret("123"));
        contextMock.Setup(x => x.Passphrase).Returns(UserPassphrase.Parse("123", PreAuthModeDescriptor.Default));
        
        //Act
        await processor.ProcessFirstFactor(contextMock.Object);
        
        //Assert
        Assert.Equal(AuthenticationStatus.Reject, authState.FirstFactorStatus);
    }
    
    [Fact]
    public async Task ProcessFirstFactor_ResponseIsNull_ShouldReject()
    {
        //Arrange
        var clientFactoryMock = new Mock<IRadiusClientFactory>();
        var clientMock = new Mock<IRadiusClient>();
        clientMock.Setup(x => x.SendPacketAsync(It.IsAny<byte>(), It.IsAny<byte[]>(), It.IsAny<IPEndPoint>(), It.IsAny<TimeSpan>())).ReturnsAsync(() => null);
        clientFactoryMock.Setup(x => x.CreateRadiusClient(It.IsAny<IPEndPoint>())).Returns(clientMock.Object);
        
        var packetService = new Mock<IRadiusPacketService>();
        packetService.Setup(x => x.GetBytes(It.IsAny<IRadiusPacket>(),It.IsAny<SharedSecret>())).Returns([]);
        var processor = new RadiusFirstFactorProcessor(packetService.Object, clientFactoryMock.Object, NullLogger<RadiusFirstFactorProcessor>.Instance);
        
        var contextMock = new Mock<IRadiusPipelineExecutionContext>();
        var authState = new AuthenticationState();
        var transformRules = new UserNameTransformRules();
        var requestPacketMock = new Mock<IRadiusPacket>();
        requestPacketMock.Setup(x => x.UserName).Returns("username");
        requestPacketMock.Setup(x => x.Code).Returns(PacketCode.AccessRequest);
        requestPacketMock.Setup(x => x.Identifier).Returns(1);
        requestPacketMock.Setup(x => x.Authenticator).Returns(new RadiusAuthenticator());
        requestPacketMock.Setup(x => x.Attributes).Returns(new Dictionary<string, RadiusAttribute>());
        
        contextMock.Setup(x => x.RequestPacket).Returns(requestPacketMock.Object);
        contextMock.Setup(x => x.AuthenticationState).Returns(authState);
        contextMock.Setup(x => x.UserNameTransformRules).Returns(transformRules);
        contextMock.Setup(x => x.NpsServerEndpoints).Returns(new HashSet<IPEndPoint>([IPEndPoint.Parse("127.0.0.1")]));
        contextMock.Setup(x => x.ServiceClientEndpoint).Returns(IPEndPoint.Parse("127.0.0.1"));
        contextMock.Setup(x => x.PreAuthnMode).Returns(PreAuthModeDescriptor.Default);
        contextMock.Setup(x => x.RadiusSharedSecret).Returns(new SharedSecret("123"));
        contextMock.Setup(x => x.Passphrase).Returns(UserPassphrase.Parse("123", PreAuthModeDescriptor.Default));
        
        //Act
        await processor.ProcessFirstFactor(contextMock.Object);
        
        //Assert
        Assert.Equal(AuthenticationStatus.Reject, authState.FirstFactorStatus);
    }
    
    [Fact]
    public async Task ProcessFirstFactor_MultipleNpsServersAndResponseIsNull_ShouldReject()
    {
        //Arrange
        var clientFactoryMock = new Mock<IRadiusClientFactory>();
        var clientMock = new Mock<IRadiusClient>();
        clientMock.Setup(x => x.SendPacketAsync(It.IsAny<byte>(), It.IsAny<byte[]>(), It.IsAny<IPEndPoint>(), It.IsAny<TimeSpan>())).ReturnsAsync(() => null);
        clientFactoryMock.Setup(x => x.CreateRadiusClient(It.IsAny<IPEndPoint>())).Returns(clientMock.Object);
        
        var packetService = new Mock<IRadiusPacketService>();
        packetService.Setup(x => x.GetBytes(It.IsAny<IRadiusPacket>(),It.IsAny<SharedSecret>())).Returns([]);
        var processor = new RadiusFirstFactorProcessor(packetService.Object, clientFactoryMock.Object, NullLogger<RadiusFirstFactorProcessor>.Instance);
        
        var contextMock = new Mock<IRadiusPipelineExecutionContext>();
        var authState = new AuthenticationState();
        var transformRules = new UserNameTransformRules();
        var requestPacketMock = new Mock<IRadiusPacket>();
        requestPacketMock.Setup(x => x.UserName).Returns("username");
        requestPacketMock.Setup(x => x.Code).Returns(PacketCode.AccessRequest);
        requestPacketMock.Setup(x => x.Identifier).Returns(1);
        requestPacketMock.Setup(x => x.Authenticator).Returns(new RadiusAuthenticator());
        requestPacketMock.Setup(x => x.Attributes).Returns(new Dictionary<string, RadiusAttribute>());
        
        contextMock.Setup(x => x.RequestPacket).Returns(requestPacketMock.Object);
        contextMock.Setup(x => x.AuthenticationState).Returns(authState);
        contextMock.Setup(x => x.UserNameTransformRules).Returns(transformRules);
        contextMock.Setup(x => x.NpsServerEndpoints).Returns(new HashSet<IPEndPoint>([IPEndPoint.Parse("127.0.0.1"), IPEndPoint.Parse("127.0.0.2")]));
        contextMock.Setup(x => x.ServiceClientEndpoint).Returns(IPEndPoint.Parse("127.0.0.1"));
        contextMock.Setup(x => x.PreAuthnMode).Returns(PreAuthModeDescriptor.Default);
        contextMock.Setup(x => x.RadiusSharedSecret).Returns(new SharedSecret("123"));
        contextMock.Setup(x => x.Passphrase).Returns(UserPassphrase.Parse("123", PreAuthModeDescriptor.Default));
        
        //Act
        await processor.ProcessFirstFactor(contextMock.Object);
        
        //Assert
        Assert.Equal(AuthenticationStatus.Reject, authState.FirstFactorStatus);
        clientMock.Verify(x => x.SendPacketAsync(It.IsAny<byte>(), It.IsAny<byte[]>(), It.IsAny<IPEndPoint>(), It.IsAny<TimeSpan>()), Times.Exactly(2));
    }
    
    [Fact]
    public async Task ProcessFirstFactor_MultipleNpsServersAndAcceptCode_ShouldAccept()
    {
        var nps1 = IPEndPoint.Parse("127.0.0.1");
        var nps2 = IPEndPoint.Parse("127.0.0.2");
        //Arrange
        var clientFactoryMock = new Mock<IRadiusClientFactory>();
        var clientMock = new Mock<IRadiusClient>();
        clientMock.Setup(x => x.SendPacketAsync(It.IsAny<byte>(), It.IsAny<byte[]>(), It.Is<IPEndPoint>(i => i == nps1), It.IsAny<TimeSpan>())).ReturnsAsync(() => null);
        clientMock.Setup(x => x.SendPacketAsync(It.IsAny<byte>(), It.IsAny<byte[]>(), It.Is<IPEndPoint>(i => i == nps2), It.IsAny<TimeSpan>())).ReturnsAsync(() => []);
        clientFactoryMock.Setup(x => x.CreateRadiusClient(It.IsAny<IPEndPoint>())).Returns(clientMock.Object);
        
        var packetService = new Mock<IRadiusPacketService>();
        packetService.Setup(x => x.GetBytes(It.IsAny<IRadiusPacket>(),It.IsAny<SharedSecret>())).Returns([]);
        var responseMock = new Mock<IRadiusPacket>();
        responseMock.Setup(x => x.Code).Returns(PacketCode.AccessAccept);
        packetService.Setup(x => x.Parse(It.IsAny<byte[]>(), It.IsAny<SharedSecret>(), It.IsAny<RadiusAuthenticator>())).Returns(responseMock.Object);
        
        var processor = new RadiusFirstFactorProcessor(packetService.Object, clientFactoryMock.Object, NullLogger<RadiusFirstFactorProcessor>.Instance);
        
        var contextMock = new Mock<IRadiusPipelineExecutionContext>();
        var authState = new AuthenticationState();
        var transformRules = new UserNameTransformRules();
        var requestPacketMock = new Mock<IRadiusPacket>();
        requestPacketMock.Setup(x => x.UserName).Returns("username");
        requestPacketMock.Setup(x => x.Code).Returns(PacketCode.AccessRequest);
        requestPacketMock.Setup(x => x.Identifier).Returns(1);
        requestPacketMock.Setup(x => x.Authenticator).Returns(new RadiusAuthenticator());
        requestPacketMock.Setup(x => x.Attributes).Returns(new Dictionary<string, RadiusAttribute>());
        
        contextMock.Setup(x => x.RequestPacket).Returns(requestPacketMock.Object);
        contextMock.Setup(x => x.AuthenticationState).Returns(authState);
        contextMock.Setup(x => x.UserNameTransformRules).Returns(transformRules);
        contextMock.Setup(x => x.NpsServerEndpoints).Returns(new HashSet<IPEndPoint>([nps1, nps2]));
        contextMock.Setup(x => x.ServiceClientEndpoint).Returns(IPEndPoint.Parse("127.0.0.1"));
        contextMock.Setup(x => x.PreAuthnMode).Returns(PreAuthModeDescriptor.Default);
        contextMock.Setup(x => x.RadiusSharedSecret).Returns(new SharedSecret("123"));
        contextMock.Setup(x => x.Passphrase).Returns(UserPassphrase.Parse("123", PreAuthModeDescriptor.Default));
        
        //Act
        await processor.ProcessFirstFactor(contextMock.Object);
        
        //Assert
        Assert.Equal(AuthenticationStatus.Accept, authState.FirstFactorStatus);
        clientMock.Verify(x => x.SendPacketAsync(It.IsAny<byte>(), It.IsAny<byte[]>(), It.IsAny<IPEndPoint>(), It.IsAny<TimeSpan>()), Times.Exactly(2));
    }
}