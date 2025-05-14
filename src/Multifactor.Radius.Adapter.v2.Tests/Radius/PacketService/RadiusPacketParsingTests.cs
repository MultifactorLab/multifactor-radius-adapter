using Microsoft.Extensions.Logging.Abstractions;
using Multifactor.Radius.Adapter.v2.Core.Radius;
using Multifactor.Radius.Adapter.v2.Core.Radius.Packet;
using Multifactor.Radius.Adapter.v2.Tests.Fixture;

namespace Multifactor.Radius.Adapter.v2.Tests.Radius.PacketService;

public class RadiusPacketParsingTests
{
    [Fact]
    public void ParseAccessRequestPacket_ShouldParse()
    {
        var dictionary = TestUtils.GetRadiusDictionary();
        var packetService = new RadiusPacketService(NullLogger<RadiusPacketService>.Instance, dictionary);
        var secret = new SharedSecret("888");
        var packet = packetService.Parse(PacketExamples.DefaultAccessRequest, secret);
        Assert.NotNull(packet);
        Assert.Equal(PacketCode.AccessRequest, packet.Code);
        Assert.Equal("TestUser", packet.GetAttributeValueAsString("User-Name"));
        Assert.Equal("TestPassword", packet.GetAttributeValueAsString("User-Password"));
    }
    
    [Fact]
    public void ParseStatusServerPacket_ShouldParse()
    {
        var dictionary = TestUtils.GetRadiusDictionary();
        var packetService = new RadiusPacketService(NullLogger<RadiusPacketService>.Instance, dictionary);
        var secret = new SharedSecret("888");
        var packet = packetService.Parse(PacketExamples.DefaultStatusServer, secret);
        Assert.NotNull(packet);
        Assert.Equal(PacketCode.StatusServer, packet.Code);
    }
    
    [Fact]
    public void ParseAccessRequestPacket_WrongSharedSecret_PasswordDoesNotMatch()
    {
        var dictionary = TestUtils.GetRadiusDictionary();
        var packetService = new RadiusPacketService(NullLogger<RadiusPacketService>.Instance, dictionary);
        var secret = new SharedSecret("999");
        var packet = packetService.Parse(PacketExamples.DefaultAccessRequest, secret);
        Assert.NotNull(packet);
        Assert.NotEqual("TestPassword", packet.GetAttributeValueAsString("User-Password"));
    }
    
    [Fact]
    public void SerializeStatusServerPacket_ShouldSerialize()
    {
        var dictionary = TestUtils.GetRadiusDictionary();
        var packetService = new RadiusPacketService(NullLogger<RadiusPacketService>.Instance, dictionary);
        var secret = new SharedSecret("888");
        var packet = packetService.Parse(PacketExamples.DefaultStatusServer, secret);
        
        var packetBytes = packetService.GetBytes(packet, secret);
        Assert.True(PacketExamples.DefaultStatusServer.SequenceEqual(packetBytes));
    }
    
    [Fact]
    public void SerializeAccessRequestPacket_ShouldSerialize()
    {
        var dictionary = TestUtils.GetRadiusDictionary();
        var packetService = new RadiusPacketService(NullLogger<RadiusPacketService>.Instance, dictionary);
        var secret = new SharedSecret("888");
        var packet = packetService.Parse(PacketExamples.DefaultAccessRequest, secret);
        
        var packetBytes = packetService.GetBytes(packet, secret);
        Assert.True(PacketExamples.DefaultAccessRequest.SequenceEqual(packetBytes));
    }
    
    [Fact]
    public void SerializeAccessRequestPacket_WrongSecret_ShouldNotMatch()
    {
        var dictionary = TestUtils.GetRadiusDictionary();
        var packetService = new RadiusPacketService(NullLogger<RadiusPacketService>.Instance, dictionary);
        var secret = new SharedSecret("999");
        var packet = packetService.Parse(PacketExamples.DefaultAccessRequest, secret);
        
        var packetBytes = packetService.GetBytes(packet, secret);
        Assert.False(PacketExamples.DefaultAccessRequest.SequenceEqual(packetBytes));
    }
}