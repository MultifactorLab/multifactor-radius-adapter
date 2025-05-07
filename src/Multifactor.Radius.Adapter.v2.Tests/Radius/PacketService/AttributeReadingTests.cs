using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using Multifactor.Radius.Adapter.v2.Core.Radius;
using Multifactor.Radius.Adapter.v2.Tests.Fixture;

namespace Multifactor.Radius.Adapter.v2.Tests.Radius.PacketService;

public class AttributeReadingTests
{
    [Fact]
    public void ReadCustomOctetAttribute_ShouldRead()
    {
        var dictionary = TestUtils.GetRadiusDictionary();
        var packetService = new RadiusPacketService(NullLogger<RadiusPacketService>.Instance, dictionary);
        var secret = new SharedSecret("888");
        var packet = packetService.Parse(PacketExamples.DefaultAccessRequest, secret);

        packet.AddAttributeValue("State", "TestState");

        var bytes = packetService.GetBytes(packet, secret);
        Assert.NotEmpty(bytes);

        packet = packetService.Parse(bytes, secret);
        Assert.Equal("TestState", packet.GetAttributeValueAsString("State"));
    }

    [Fact]
    public void ReadCustomTaggedStringAttribute_ShouldRead()
    {
        var dictionary = TestUtils.GetRadiusDictionary();
        var packetService = new RadiusPacketService(NullLogger<RadiusPacketService>.Instance, dictionary);
        var secret = new SharedSecret("888");
        var packet = packetService.Parse(PacketExamples.DefaultAccessRequest, secret);

        packet.AddAttributeValue("Tunnel-Client-Auth-ID", "Test-Tunnel-Client-Auth-ID");

        var bytes = packetService.GetBytes(packet, secret);
        Assert.NotEmpty(bytes);

        packet = packetService.Parse(bytes, secret);
        Assert.Equal("Test-Tunnel-Client-Auth-ID", packet.GetAttribute<string>("Tunnel-Client-Auth-ID"));
        Assert.Equal("Test-Tunnel-Client-Auth-ID", packet.GetAttributeValueAsString("Tunnel-Client-Auth-ID"));
    }

    [Fact]
    public void ReadCustomIntegerAttribute_ShouldRead()
    {
        var dictionary = TestUtils.GetRadiusDictionary();
        var packetService = new RadiusPacketService(NullLogger<RadiusPacketService>.Instance, dictionary);
        var secret = new SharedSecret("888");
        var packet = packetService.Parse(PacketExamples.DefaultAccessRequest, secret);

        packet.AddAttributeValue("NAS-Port", 123456);

        var bytes = packetService.GetBytes(packet, secret);
        Assert.NotEmpty(bytes);

        packet = packetService.Parse(bytes, secret);
        Assert.Equal(123456, packet.GetAttribute<int>("NAS-Port"));
    }

    [Fact]
    public void ReadCustomTaggedIntegerAttribute_ShouldRead()
    {
        var dictionary = TestUtils.GetRadiusDictionary();
        var packetService = new RadiusPacketService(NullLogger<RadiusPacketService>.Instance, dictionary);
        var secret = new SharedSecret("888");
        var packet = packetService.Parse(PacketExamples.DefaultAccessRequest, secret);
        packet.AddAttributeValue("Tunnel-Preference", 123456);

        var bytes = packetService.GetBytes(packet, secret);
        Assert.NotEmpty(bytes);

        packet = packetService.Parse(bytes, secret);
        Assert.Equal(123456, packet.GetAttribute<int>("Tunnel-Preference"));
    }

    [Fact]
    public void ReadCustomIpAddrAttribute_ShouldRead()
    {
        var dictionary = TestUtils.GetRadiusDictionary();
        var packetService = new RadiusPacketService(NullLogger<RadiusPacketService>.Instance, dictionary);
        var secret = new SharedSecret("888");
        var packet = packetService.Parse(PacketExamples.DefaultAccessRequest, secret);
        var ipAddr = IPAddress.Parse("127.0.0.1");
        packet.AddAttributeValue("NAS-IP-Address", ipAddr);

        var bytes = packetService.GetBytes(packet, secret);
        Assert.NotEmpty(bytes);

        packet = packetService.Parse(bytes, secret);
        Assert.Equal(ipAddr, packet.GetAttribute<IPAddress>("NAS-IP-Address"));
    }

    [Fact]
    public void ReadCustomIpAddrAttribute_TwoValues_ShouldRead()
    {
        var dictionary = TestUtils.GetRadiusDictionary();
        var packetService = new RadiusPacketService(NullLogger<RadiusPacketService>.Instance, dictionary);
        var secret = new SharedSecret("888");
        var packet = packetService.Parse(PacketExamples.DefaultAccessRequest, secret);

        var ipAddr1 = IPAddress.Parse("127.0.0.1");
        var ipAddr2 = IPAddress.Parse("127.0.0.2");
        packet.AddAttributeValue("NAS-IP-Address", ipAddr1);
        packet.AddAttributeValue("NAS-IP-Address", ipAddr2);

        var bytes = packetService.GetBytes(packet, secret);
        Assert.NotEmpty(bytes);

        packet = packetService.Parse(bytes, secret);
        var attributes = packet.GetAttributes<IPAddress>("NAS-IP-Address");
        Assert.Collection(
            attributes,
            e => Assert.Equal(ipAddr1, e),
            e => Assert.Equal(ipAddr2, e));
    }

    [Fact]
    public void ReadCustomStringAttribute_TwoValues_ShouldRead()
    {
        var dictionary = TestUtils.GetRadiusDictionary();
        var packetService = new RadiusPacketService(NullLogger<RadiusPacketService>.Instance, dictionary);
        var secret = new SharedSecret("888");
        var packet = packetService.Parse(PacketExamples.DefaultAccessRequest, secret);

        var str1 = "Test-1-Tunnel-Client-Auth-ID";
        var str2 = "Test-2-Tunnel-Client-Auth-ID";
        packet.AddAttributeValue("Tunnel-Client-Auth-ID", str1);
        packet.AddAttributeValue("Tunnel-Client-Auth-ID", str2);

        var bytes = packetService.GetBytes(packet, secret);
        Assert.NotEmpty(bytes);

        packet = packetService.Parse(bytes, secret);
        var attributes = packet.GetAttributes<string>("Tunnel-Client-Auth-ID");
        Assert.Collection(
            attributes,
            e => Assert.Equal(str1, e),
            e => Assert.Equal(str2, e));
    }
}