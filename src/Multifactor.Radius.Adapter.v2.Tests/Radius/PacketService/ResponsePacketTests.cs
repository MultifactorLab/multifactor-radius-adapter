using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using Multifactor.Radius.Adapter.v2.Core.Radius;
using Multifactor.Radius.Adapter.v2.Core.Radius.Packet;
using Multifactor.Radius.Adapter.v2.Tests.Fixture;

namespace Multifactor.Radius.Adapter.v2.Tests.Radius.PacketService;

public class ResponsePacketTests
{
    [Fact]
    public void CreateResponsePacket_ShouldCreateResponsePacket()
    {
        var dictionary = TestUtils.GetRadiusDictionary();
        var packetService = new RadiusPacketService(NullLogger<RadiusPacketService>.Instance, dictionary);
        var secret = new SharedSecret("888");
        var packet = packetService.Parse(PacketExamples.DefaultAccessRequest, secret);

        var responsePacket = packetService.CreateResponsePacket(packet, PacketCode.AccountingRequest);

        Assert.NotNull(responsePacket);
        Assert.Equal(PacketCode.AccountingRequest, responsePacket.Code);
        Assert.True(packet.Authenticator!.Value.SequenceEqual(responsePacket.RequestAuthenticator!.Value));
        Assert.Equal(packet.Identifier, responsePacket.Identifier);
    }

    [Fact]
    public void SerializeResponsePacket_ShouldSerializeResponsePacket()
    {
        var dictionary = TestUtils.GetRadiusDictionary();
        var packetService = new RadiusPacketService(NullLogger<RadiusPacketService>.Instance, dictionary);
        var secret = new SharedSecret("888");
        var packet = packetService.Parse(PacketExamples.DefaultAccessRequest, secret);

        var responsePacket = packetService.CreateResponsePacket(packet, PacketCode.AccountingRequest);
        var responsePacketBytes = packetService.GetBytes(responsePacket, secret);
        Assert.NotNull(responsePacketBytes);

        var deserialized = packetService.Parse(responsePacketBytes, secret);
        Assert.NotNull(deserialized);

        Assert.Equal(responsePacket.Identifier, deserialized.Identifier);
        Assert.Equal(PacketCode.AccountingRequest, responsePacket.Code);
    }
    
    [Fact]
    public void SerializeResponsePacket_HasCustomAttributes_ShouldSerializeResponsePacket()
    {
        var dictionary = TestUtils.GetRadiusDictionary();
        var packetService = new RadiusPacketService(NullLogger<RadiusPacketService>.Instance, dictionary);
        var secret = new SharedSecret("888");
        var packet = packetService.Parse(PacketExamples.DefaultAccessRequest, secret);

        var responsePacket = packetService.CreateResponsePacket(packet, PacketCode.AccountingRequest);
        
        var ipAddr = IPAddress.Parse("127.0.0.1");
        responsePacket.AddAttributeValue("State", "TestState");
        responsePacket.AddAttributeValue("NAS-IP-Address", ipAddr);
        
        var responsePacketBytes = packetService.GetBytes(responsePacket, secret);
        Assert.NotNull(responsePacketBytes);

        var deserialized = packetService.Parse(responsePacketBytes, secret);
        Assert.NotNull(deserialized);

        Assert.Equal(ipAddr, deserialized.GetAttribute<IPAddress>("NAS-IP-Address"));
        Assert.Equal("TestState", deserialized.GetAttributeValueAsString("State"));
    }
}