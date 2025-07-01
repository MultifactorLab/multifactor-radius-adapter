using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Multifactor.Radius.Adapter.v2.Core.Auth;
using Multifactor.Radius.Adapter.v2.Core.Configuration.Client;
using Multifactor.Radius.Adapter.v2.Core.Pipeline;
using Multifactor.Radius.Adapter.v2.Core.Radius;
using Multifactor.Radius.Adapter.v2.Core.Radius.Packet;
using Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline.Context;
using Multifactor.Radius.Adapter.v2.Server.Udp;
using Multifactor.Radius.Adapter.v2.Services.AdapterResponseSender;
using Multifactor.Radius.Adapter.v2.Services.Radius;

namespace Multifactor.Radius.Adapter.v2.Tests.Unit;

public class AdapterResponseSenderTests
{
    [Fact]
    public async Task SendResponse_ShouldSkipResponse()
    {
        var contextMock = new Mock<IRadiusPipelineExecutionContext>();
        contextMock.Setup(x => x.ResponsePacket).Returns(() => null);
        contextMock.Setup(x => x.RemoteEndpoint).Returns(IPEndPoint.Parse("127.0.0.1"));
        contextMock.SetupProperty(x => x.AuthenticationState);
        contextMock.Setup(x => x.RadiusSharedSecret).Returns(new SharedSecret("123"));
        contextMock.Setup(x => x.UserGroups).Returns([]);
        contextMock.Setup(x => x.RadiusReplyAttributes).Returns(new Dictionary<string, RadiusReplyAttributeValue[]>());
        contextMock.Setup(x => x.ExecutionState.ShouldSkipResponse).Returns(true);
        contextMock.Setup(x => x.UserLdapProfile.Attributes).Returns([]);
        contextMock.Setup(x => x.AuthenticationState).Returns(new AuthenticationState());
        contextMock.Setup(x => x.ResponseInformation).Returns(new ResponseInformation());
        contextMock.Setup(x => x.RequestPacket).Returns(new Mock<IRadiusPacket>().Object);
        
        var packetServiceMock = new Mock<IRadiusPacketService>();
        var attributeServiceMock = new Mock<IRadiusReplyAttributeService>();
        var udpClientMock = new Mock<IUdpClient>();

        var sender = new AdapterResponseSender(packetServiceMock.Object, udpClientMock.Object, attributeServiceMock.Object, NullLogger<AdapterResponseSender>.Instance);
        var request = new SendAdapterResponseRequest(contextMock.Object);
        
        await sender.SendResponse(request);
        
        udpClientMock.Verify(x =>  x.SendAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<IPEndPoint>()), Times.Never);
    }
    
    [Fact]
    public async Task SendResponse_EapMessageChallenge_ShouldSendResponse()
    {
        var contextMock = new Mock<IRadiusPipelineExecutionContext>();
        
        contextMock.SetupProperty(x => x.AuthenticationState);
        contextMock.Setup(x => x.UserGroups).Returns([]);
        contextMock.Setup(x => x.RadiusReplyAttributes).Returns(new Dictionary<string, RadiusReplyAttributeValue[]>());
        contextMock.Setup(x => x.ExecutionState.ShouldSkipResponse).Returns(false);
        contextMock.Setup(x => x.ResponsePacket!.IsEapMessageChallenge).Returns(true);
        contextMock.Setup(x => x.RemoteEndpoint).Returns(IPEndPoint.Parse("127.0.0.1"));
        contextMock.Setup(x => x.RequestPacket.Identifier).Returns(1);
        contextMock.Setup(x => x.RadiusSharedSecret).Returns(new SharedSecret("123"));
        contextMock.Setup(x => x.UserLdapProfile.Attributes).Returns([]);
        contextMock.Setup(x => x.AuthenticationState).Returns(new AuthenticationState());
        contextMock.Setup(x => x.ResponseInformation).Returns(new ResponseInformation());
        
        var packetServiceMock = new Mock<IRadiusPacketService>();
        var attributeServiceMock = new Mock<IRadiusReplyAttributeService>();
        var udpClientMock = new Mock<IUdpClient>();

        var sender = new AdapterResponseSender(packetServiceMock.Object, udpClientMock.Object, attributeServiceMock.Object, NullLogger<AdapterResponseSender>.Instance);
        var responsePacketMock = new Mock<IRadiusPacket>();
        responsePacketMock.Setup(x => x.IsEapMessageChallenge).Returns(true);
        var requestPacketMock = new Mock<IRadiusPacket>();
        requestPacketMock.Setup(x=> x.Identifier).Returns(1);
        
        var request = new SendAdapterResponseRequest(contextMock.Object);
        
        await sender.SendResponse(request);
        
        udpClientMock.Verify(x =>  x.SendAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<IPEndPoint>()), Times.Once);
    }
    
    [Fact]
    public async Task SendResponse_VendorAclRequest_ShouldSendResponse()
    {
        var contextMock = new Mock<IRadiusPipelineExecutionContext>();
        contextMock.Setup(x => x.ExecutionState.ShouldSkipResponse).Returns(false);
        contextMock.Setup(x => x.ResponsePacket).Returns(new Mock<IRadiusPacket>().Object);
        contextMock.Setup(x => x.ResponsePacket!.IsEapMessageChallenge).Returns(false);
        contextMock.Setup(x => x.RemoteEndpoint).Returns(IPEndPoint.Parse("127.0.0.1"));
        contextMock.Setup(x => x.RequestPacket.Identifier).Returns(1);
        contextMock.Setup(x => x.RequestPacket.IsVendorAclRequest).Returns(true);
        contextMock.Setup(x => x.RadiusSharedSecret).Returns(new SharedSecret("123"));
        contextMock.Setup(x => x.UserLdapProfile.Attributes).Returns([]);
        contextMock.Setup(x => x.AuthenticationState).Returns(new AuthenticationState());
        contextMock.Setup(x => x.ResponseInformation).Returns(new ResponseInformation());
        contextMock.Setup(x => x.UserGroups).Returns([]);
        contextMock.Setup(x => x.RadiusReplyAttributes).Returns(new Dictionary<string, RadiusReplyAttributeValue[]>());
        var packetServiceMock = new Mock<IRadiusPacketService>();
        var attributeServiceMock = new Mock<IRadiusReplyAttributeService>();
        var udpClientMock = new Mock<IUdpClient>();

        var sender = new AdapterResponseSender(packetServiceMock.Object, udpClientMock.Object, attributeServiceMock.Object, NullLogger<AdapterResponseSender>.Instance);
        var request = new SendAdapterResponseRequest(contextMock.Object);
        await sender.SendResponse(request);
        
        udpClientMock.Verify(x =>  x.SendAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<IPEndPoint>()), Times.Once);
    }
    
    [Fact]
    public async Task SendResponse_AccessAcceptNoResponsePacket_ShouldSendResponse()
    {
        var contextMock = new Mock<IRadiusPipelineExecutionContext>();
        contextMock.Setup(x => x.ExecutionState.ShouldSkipResponse).Returns(false);
        contextMock.Setup(x => x.ResponsePacket).Returns(() => null);
        contextMock.Setup(x => x.RemoteEndpoint).Returns(IPEndPoint.Parse("127.0.0.1"));
        contextMock.Setup(x => x.RequestPacket.Identifier).Returns(1);
        contextMock.Setup(x => x.RequestPacket.IsVendorAclRequest).Returns(false);
        contextMock.SetupProperty(x => x.AuthenticationState);
        contextMock.Setup(x => x.RadiusSharedSecret).Returns(new SharedSecret("123"));
        contextMock.Setup(x => x.RequestPacket.UserName).Returns("userName");
        contextMock.Setup(x => x.RequestPacket.Attributes).Returns(new Dictionary<string, RadiusAttribute>());
        contextMock.Setup(x => x.UserGroups).Returns([]);
        contextMock.Setup(x => x.RadiusReplyAttributes).Returns(new Dictionary<string, RadiusReplyAttributeValue[]>());
        contextMock.Setup(x => x.UserLdapProfile.Attributes).Returns([]);
        contextMock.Setup(x => x.ResponseInformation.ReplyMessage).Returns("replyMessage");
        contextMock.Setup(x => x.ResponseInformation.State).Returns("state");
        var context = contextMock.Object;
        context.AuthenticationState = new AuthenticationState();
        context.AuthenticationState.FirstFactorStatus = AuthenticationStatus.Accept;
        context.AuthenticationState.SecondFactorStatus = AuthenticationStatus.Accept;
        
        var packetServiceMock = new Mock<IRadiusPacketService>();
        var packet = new RadiusPacket(new RadiusPacketHeader(PacketCode.AccessAccept, 1, new byte[16]));
        var packetBytes = new byte[] {1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16};
        packetServiceMock.Setup(x => x.CreateResponsePacket(It.IsAny<IRadiusPacket>(), PacketCode.AccessAccept)).Returns(packet);
        packetServiceMock.Setup(x => x.GetBytes(packet, It.IsAny<SharedSecret>())).Returns(packetBytes);
        
        var attributeServiceMock = new Mock<IRadiusReplyAttributeService>();
        attributeServiceMock.Setup(x => x.GetReplyAttributes(It.IsAny<GetReplyAttributesRequest>())).Returns(new Dictionary<string, List<object>>());
        var udpClientMock = new Mock<IUdpClient>();

        var sender = new AdapterResponseSender(packetServiceMock.Object, udpClientMock.Object, attributeServiceMock.Object, NullLogger<AdapterResponseSender>.Instance);
        var request = new SendAdapterResponseRequest(context);
        await sender.SendResponse(request);
        
        udpClientMock.Verify(x =>  x.SendAsync(packetBytes, It.IsAny<int>(), It.IsAny<IPEndPoint>()), Times.Once);
    }
    
    [Fact]
    public async Task SendResponse_AccessAcceptHasResponsePacket_ShouldSendResponse()
    {
        var contextMock = new Mock<IRadiusPipelineExecutionContext>();
        var responsePacketMock = new Mock<IRadiusPacket>();
        responsePacketMock.Setup(x => x.Attributes).Returns(new Dictionary<string, RadiusAttribute>());
        
        contextMock.Setup(x => x.ExecutionState.ShouldSkipResponse).Returns(false);
        contextMock.Setup(x => x.ResponsePacket).Returns(responsePacketMock.Object);
        contextMock.Setup(x => x.RemoteEndpoint).Returns(IPEndPoint.Parse("127.0.0.1"));
        contextMock.Setup(x => x.RequestPacket).Returns(new Mock<IRadiusPacket>().Object);
        contextMock.Setup(x => x.RequestPacket.Identifier).Returns(1);
        contextMock.Setup(x => x.RequestPacket.IsVendorAclRequest).Returns(false);
        contextMock.Setup(x => x.RequestPacket.UserName).Returns("userName");
        contextMock.Setup(x => x.RequestPacket.Attributes).Returns(new Dictionary<string, RadiusAttribute>());

        contextMock.SetupProperty(x => x.AuthenticationState);
        contextMock.Setup(x => x.RadiusSharedSecret).Returns(new SharedSecret("123"));

        contextMock.Setup(x => x.UserGroups).Returns([]);
        contextMock.Setup(x => x.RadiusReplyAttributes).Returns(new Dictionary<string, RadiusReplyAttributeValue[]>());
        contextMock.Setup(x => x.UserLdapProfile.Attributes).Returns([]);
        contextMock.Setup(x => x.ResponseInformation.ReplyMessage).Returns("replyMessage");
        contextMock.Setup(x => x.ResponseInformation.State).Returns("state");
        var context = contextMock.Object;
        context.AuthenticationState = new AuthenticationState();
        context.AuthenticationState.FirstFactorStatus = AuthenticationStatus.Accept;
        context.AuthenticationState.SecondFactorStatus = AuthenticationStatus.Accept;
        
        var packetServiceMock = new Mock<IRadiusPacketService>();
        var packet = new RadiusPacket(new RadiusPacketHeader(PacketCode.AccessAccept, 1, new byte[16]));
        var packetBytes = new byte[] {1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16};
        packetServiceMock.Setup(x => x.CreateResponsePacket(It.IsAny<IRadiusPacket>(), PacketCode.AccessAccept)).Returns(packet);
        packetServiceMock.Setup(x => x.GetBytes(packet, It.IsAny<SharedSecret>())).Returns(packetBytes);
        
        var attributeServiceMock = new Mock<IRadiusReplyAttributeService>();
        attributeServiceMock.Setup(x => x.GetReplyAttributes(It.IsAny<GetReplyAttributesRequest>())).Returns(new Dictionary<string, List<object>>());
        var udpClientMock = new Mock<IUdpClient>();

        var sender = new AdapterResponseSender(packetServiceMock.Object, udpClientMock.Object, attributeServiceMock.Object, NullLogger<AdapterResponseSender>.Instance);
        var request = new SendAdapterResponseRequest(context);
        
        await sender.SendResponse(request);
        
        udpClientMock.Verify(x =>  x.SendAsync(packetBytes, It.IsAny<int>(), It.IsAny<IPEndPoint>()), Times.Once);
    }
    
    [Fact]
    public async Task SendResponse_AccessRejectHasResponsePacket_ShouldSendResponse()
    {
        var contextMock = new Mock<IRadiusPipelineExecutionContext>();
        var responsePacketMock = new Mock<IRadiusPacket>();
        responsePacketMock.Setup(x => x.Attributes).Returns(new Dictionary<string, RadiusAttribute>());
        responsePacketMock.Setup(x => x.Code).Returns(PacketCode.AccessReject);
        
        contextMock.Setup(x => x.ExecutionState.ShouldSkipResponse).Returns(false);
        contextMock.Setup(x => x.ResponsePacket).Returns(responsePacketMock.Object);
        contextMock.Setup(x => x.RemoteEndpoint).Returns(IPEndPoint.Parse("127.0.0.1"));
        contextMock.Setup(x => x.RequestPacket.Identifier).Returns(1);
        contextMock.Setup(x => x.RequestPacket.IsVendorAclRequest).Returns(false);
        contextMock.SetupProperty(x => x.AuthenticationState);
        contextMock.Setup(x => x.RadiusSharedSecret).Returns(new SharedSecret("123"));
        contextMock.Setup(x => x.RequestPacket.UserName).Returns("userName");
        contextMock.Setup(x => x.RequestPacket.Attributes).Returns(new Dictionary<string, RadiusAttribute>());
        contextMock.Setup(x => x.UserGroups).Returns([]);
        contextMock.Setup(x => x.RadiusReplyAttributes).Returns(new Dictionary<string, RadiusReplyAttributeValue[]>());
        contextMock.Setup(x => x.UserLdapProfile.Attributes).Returns([]);
        contextMock.Setup(x => x.ResponseInformation.ReplyMessage).Returns("replyMessage");
        contextMock.Setup(x => x.ResponseInformation.State).Returns("state");
        var context = contextMock.Object;
        context.AuthenticationState = new AuthenticationState();
        context.AuthenticationState.FirstFactorStatus = AuthenticationStatus.Reject;
        context.AuthenticationState.SecondFactorStatus = AuthenticationStatus.Reject;
        
        var packetServiceMock = new Mock<IRadiusPacketService>();
        var packet = new RadiusPacket(new RadiusPacketHeader(PacketCode.AccessReject, 1, new byte[16]));
        var packetBytes = new byte[] {1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16};
        packetServiceMock.Setup(x => x.CreateResponsePacket(It.IsAny<IRadiusPacket>(), PacketCode.AccessReject)).Returns(packet);
        packetServiceMock.Setup(x => x.GetBytes(packet, It.IsAny<SharedSecret>())).Returns(packetBytes);
        
        var attributeServiceMock = new Mock<IRadiusReplyAttributeService>();
        attributeServiceMock.Setup(x => x.GetReplyAttributes(It.IsAny<GetReplyAttributesRequest>())).Returns(new Dictionary<string, List<object>>());
        
        var udpClientMock = new Mock<IUdpClient>();
        var sender = new AdapterResponseSender(packetServiceMock.Object, udpClientMock.Object, attributeServiceMock.Object, NullLogger<AdapterResponseSender>.Instance);
        var request = new SendAdapterResponseRequest(context);
        
        await sender.SendResponse(request);
        
        udpClientMock.Verify(x =>  x.SendAsync(packetBytes, It.IsAny<int>(), It.IsAny<IPEndPoint>()), Times.Once);
    }
    
    [Fact]
    public async Task SendResponse_AccessRejectNoResponsePacket_ShouldSendResponse()
    {
        var contextMock = new Mock<IRadiusPipelineExecutionContext>();
        
        contextMock.Setup(x => x.ExecutionState.ShouldSkipResponse).Returns(false);
        contextMock.Setup(x => x.ResponsePacket).Returns(() => null);
        contextMock.Setup(x => x.RemoteEndpoint).Returns(IPEndPoint.Parse("127.0.0.1"));
        contextMock.Setup(x => x.RequestPacket.Identifier).Returns(1);
        contextMock.Setup(x => x.RequestPacket.IsVendorAclRequest).Returns(false);
        contextMock.SetupProperty(x => x.AuthenticationState);
        contextMock.Setup(x => x.RadiusSharedSecret).Returns(new SharedSecret("123"));
        contextMock.Setup(x => x.RequestPacket.UserName).Returns("userName");
        contextMock.Setup(x => x.RequestPacket.Attributes).Returns(new Dictionary<string, RadiusAttribute>());
        contextMock.Setup(x => x.UserGroups).Returns([]);
        contextMock.Setup(x => x.RadiusReplyAttributes).Returns(new Dictionary<string, RadiusReplyAttributeValue[]>());
        contextMock.Setup(x => x.UserLdapProfile.Attributes).Returns([]);
        contextMock.Setup(x => x.ResponseInformation.ReplyMessage).Returns("replyMessage");
        contextMock.Setup(x => x.ResponseInformation.State).Returns("state");
        var context = contextMock.Object;
        context.AuthenticationState = new AuthenticationState();
        context.AuthenticationState.FirstFactorStatus = AuthenticationStatus.Reject;
        context.AuthenticationState.SecondFactorStatus = AuthenticationStatus.Reject;
        
        var packetServiceMock = new Mock<IRadiusPacketService>();
        var packet = new RadiusPacket(new RadiusPacketHeader(PacketCode.AccessReject, 1, new byte[16]));
        var packetBytes = new byte[] {1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16};
        packetServiceMock.Setup(x => x.CreateResponsePacket(It.IsAny<IRadiusPacket>(), PacketCode.AccessReject)).Returns(packet);
        packetServiceMock.Setup(x => x.GetBytes(packet, It.IsAny<SharedSecret>())).Returns(packetBytes);
        
        var attributeServiceMock = new Mock<IRadiusReplyAttributeService>();
        attributeServiceMock.Setup(x => x.GetReplyAttributes(It.IsAny<GetReplyAttributesRequest>())).Returns(new Dictionary<string, List<object>>());
        
        var udpClientMock = new Mock<IUdpClient>();
        var sender = new AdapterResponseSender(packetServiceMock.Object, udpClientMock.Object, attributeServiceMock.Object, NullLogger<AdapterResponseSender>.Instance);
        var request = new SendAdapterResponseRequest(context);
        await sender.SendResponse(request);
        
        udpClientMock.Verify(x =>  x.SendAsync(packetBytes, It.IsAny<int>(), It.IsAny<IPEndPoint>()), Times.Once);
    }
    
    [Fact]
    public async Task SendResponse_AccessAccept_ShouldAddResponseAttributes()
    {
        var contextMock = new Mock<IRadiusPipelineExecutionContext>();
        var responsePacketMock = new Mock<IRadiusPacket>();
        var attribute = new RadiusAttribute("key");
        attribute.AddValues("customValue");
        responsePacketMock.Setup(x => x.Attributes.Values).Returns([attribute]);
        
        contextMock.Setup(x => x.ExecutionState.ShouldSkipResponse).Returns(false);
        contextMock.Setup(x => x.ResponsePacket).Returns(responsePacketMock.Object);
        contextMock.Setup(x => x.RemoteEndpoint).Returns(IPEndPoint.Parse("127.0.0.1"));
        contextMock.Setup(x => x.RequestPacket.Identifier).Returns(1);
        contextMock.Setup(x => x.RequestPacket.IsVendorAclRequest).Returns(false);
        contextMock.SetupProperty(x => x.AuthenticationState);
        contextMock.Setup(x => x.RadiusSharedSecret).Returns(new SharedSecret("123"));
        contextMock.Setup(x => x.RequestPacket.UserName).Returns("userName");
        contextMock.Setup(x => x.RequestPacket.Attributes).Returns(new Dictionary<string, RadiusAttribute>());
        contextMock.Setup(x => x.UserGroups).Returns([]);
        contextMock.Setup(x => x.RadiusReplyAttributes).Returns(new Dictionary<string, RadiusReplyAttributeValue[]>());
        contextMock.Setup(x => x.UserLdapProfile.Attributes).Returns([]);
        contextMock.Setup(x => x.ResponseInformation.ReplyMessage).Returns("replyMessage");
        contextMock.Setup(x => x.ResponseInformation.State).Returns("state");
        
        var context = contextMock.Object;
        context.AuthenticationState = new AuthenticationState();
        context.AuthenticationState.FirstFactorStatus = AuthenticationStatus.Accept;
        context.AuthenticationState.SecondFactorStatus = AuthenticationStatus.Accept;
        
        var packetServiceMock = new Mock<IRadiusPacketService>();
        var packet = new RadiusPacket(new RadiusPacketHeader(PacketCode.AccessAccept, 1, new byte[16]));
        var packetBytes = new byte[] {1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16};
        packetServiceMock.Setup(x => x.CreateResponsePacket(It.IsAny<IRadiusPacket>(), PacketCode.AccessAccept)).Returns(packet);
        packetServiceMock.Setup(x => x.GetBytes(packet, It.IsAny<SharedSecret>())).Returns(packetBytes);
        
        var attributeServiceMock = new Mock<IRadiusReplyAttributeService>();
        attributeServiceMock.Setup(x => x.GetReplyAttributes(It.IsAny<GetReplyAttributesRequest>())).Returns(new Dictionary<string, List<object>>());
        var udpClientMock = new Mock<IUdpClient>();

        var sender = new AdapterResponseSender(packetServiceMock.Object, udpClientMock.Object, attributeServiceMock.Object, NullLogger<AdapterResponseSender>.Instance);
        var request = new SendAdapterResponseRequest(context);
        await sender.SendResponse(request);
        
        udpClientMock.Verify(x =>  x.SendAsync(packetBytes, It.IsAny<int>(), It.IsAny<IPEndPoint>()), Times.Once);
        Assert.True(packet.Attributes.ContainsKey("key"));
    }
    
    [Fact]
    public async Task SendResponse_AccessAccept_ShouldAddReplyAttributes()
    {
        var contextMock = new Mock<IRadiusPipelineExecutionContext>();
        var responsePacketMock = new Mock<IRadiusPacket>();
        responsePacketMock.Setup(x => x.Attributes.Values).Returns([]);
        contextMock.Setup(x => x.ExecutionState.ShouldSkipResponse).Returns(false);
        contextMock.Setup(x => x.ResponsePacket).Returns(responsePacketMock.Object);
        contextMock.Setup(x => x.RemoteEndpoint).Returns(IPEndPoint.Parse("127.0.0.1"));
        contextMock.Setup(x => x.RequestPacket.Identifier).Returns(1);
        contextMock.Setup(x => x.RequestPacket.IsVendorAclRequest).Returns(false);
        contextMock.SetupProperty(x => x.AuthenticationState);
        contextMock.Setup(x => x.RadiusSharedSecret).Returns(new SharedSecret("123"));
        contextMock.Setup(x => x.RequestPacket.UserName).Returns("userName");
        contextMock.Setup(x => x.RequestPacket.Attributes).Returns(new Dictionary<string, RadiusAttribute>());
        contextMock.Setup(x => x.UserGroups).Returns([]);
        contextMock.Setup(x => x.RadiusReplyAttributes).Returns(new Dictionary<string, RadiusReplyAttributeValue[]>());
        contextMock.Setup(x => x.UserLdapProfile.Attributes).Returns([]);
        contextMock.Setup(x => x.ResponseInformation.ReplyMessage).Returns("replyMessage");
        contextMock.Setup(x => x.ResponseInformation.State).Returns("state");
        
        var context = contextMock.Object;
        context.AuthenticationState = new AuthenticationState();
        context.AuthenticationState.FirstFactorStatus = AuthenticationStatus.Accept;
        context.AuthenticationState.SecondFactorStatus = AuthenticationStatus.Accept;
        
        var packetServiceMock = new Mock<IRadiusPacketService>();
        var packet = new RadiusPacket(new RadiusPacketHeader(PacketCode.AccessAccept, 1, new byte[16]));
        var packetBytes = new byte[] {1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16};
        packetServiceMock.Setup(x => x.CreateResponsePacket(It.IsAny<IRadiusPacket>(), PacketCode.AccessAccept)).Returns(packet);
        packetServiceMock.Setup(x => x.GetBytes(packet, It.IsAny<SharedSecret>())).Returns(packetBytes);
        
        var attributeServiceMock = new Mock<IRadiusReplyAttributeService>();
        var replyAttributes = new Dictionary<string, List<object>>();
        replyAttributes.Add("key", new List<object>() { 123 });
        attributeServiceMock.Setup(x => x.GetReplyAttributes(It.IsAny<GetReplyAttributesRequest>())).Returns(replyAttributes);
        var udpClientMock = new Mock<IUdpClient>();
        
        var sender = new AdapterResponseSender(packetServiceMock.Object, udpClientMock.Object, attributeServiceMock.Object, NullLogger<AdapterResponseSender>.Instance);
        var request = new SendAdapterResponseRequest(context);
        
        await sender.SendResponse(request);
        
        udpClientMock.Verify(x =>  x.SendAsync(packetBytes, It.IsAny<int>(), It.IsAny<IPEndPoint>()), Times.Once);
        Assert.True(packet.Attributes.ContainsKey("key"));
    }
    
    [Fact]
    public async Task SendResponse_AccessRejectHasResponsePacket_ShouldAddResponseAttributes()
    {
        var contextMock = new Mock<IRadiusPipelineExecutionContext>();
        var responsePacketMock = new Mock<IRadiusPacket>();
        var attribute = new RadiusAttribute("key");
        attribute.AddValues("customValue");
        responsePacketMock.Setup(x => x.Attributes.Values).Returns([attribute]);
        responsePacketMock.Setup(x => x.Code).Returns(PacketCode.AccessReject);
        
        contextMock.Setup(x => x.ExecutionState.ShouldSkipResponse).Returns(false);
        contextMock.Setup(x => x.ResponsePacket).Returns(responsePacketMock.Object);
        contextMock.Setup(x => x.RemoteEndpoint).Returns(IPEndPoint.Parse("127.0.0.1"));
        contextMock.Setup(x => x.RequestPacket.Identifier).Returns(1);
        contextMock.Setup(x => x.RequestPacket.IsVendorAclRequest).Returns(false);
        contextMock.SetupProperty(x => x.AuthenticationState);
        contextMock.Setup(x => x.RadiusSharedSecret).Returns(new SharedSecret("123"));
        contextMock.Setup(x => x.RequestPacket.UserName).Returns("userName");
        contextMock.Setup(x => x.RequestPacket.Attributes).Returns(new Dictionary<string, RadiusAttribute>());
        contextMock.Setup(x => x.UserGroups).Returns([]);
        contextMock.Setup(x => x.RadiusReplyAttributes).Returns(new Dictionary<string, RadiusReplyAttributeValue[]>());
        contextMock.Setup(x => x.UserLdapProfile.Attributes).Returns([]);
        contextMock.Setup(x => x.ResponseInformation.ReplyMessage).Returns("replyMessage");
        contextMock.Setup(x => x.ResponseInformation.State).Returns("state");
        var context = contextMock.Object;
        context.AuthenticationState = new AuthenticationState();
        context.AuthenticationState.FirstFactorStatus = AuthenticationStatus.Reject;
        context.AuthenticationState.SecondFactorStatus = AuthenticationStatus.Reject;
        
        var packetServiceMock = new Mock<IRadiusPacketService>();
        var packet = new RadiusPacket(new RadiusPacketHeader(PacketCode.AccessReject, 1, new byte[16]));
        var packetBytes = new byte[] {1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16};
        packetServiceMock.Setup(x => x.CreateResponsePacket(It.IsAny<IRadiusPacket>(), PacketCode.AccessReject)).Returns(packet);
        packetServiceMock.Setup(x => x.GetBytes(packet, It.IsAny<SharedSecret>())).Returns(packetBytes);
        var attributeServiceMock = new Mock<IRadiusReplyAttributeService>();
        attributeServiceMock.Setup(x => x.GetReplyAttributes(It.IsAny<GetReplyAttributesRequest>())).Returns(new Dictionary<string, List<object>>());
        
        var udpClientMock = new Mock<IUdpClient>();
        
        var sender = new AdapterResponseSender(packetServiceMock.Object, udpClientMock.Object, attributeServiceMock.Object, NullLogger<AdapterResponseSender>.Instance);
        var request = new SendAdapterResponseRequest(context);
        await sender.SendResponse(request);
        
        udpClientMock.Verify(x =>  x.SendAsync(packetBytes, It.IsAny<int>(), It.IsAny<IPEndPoint>()), Times.Once);
        Assert.True(packet.Attributes.ContainsKey("key"));
    }
}