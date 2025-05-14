using Microsoft.Extensions.Logging.Abstractions;
using Multifactor.Radius.Adapter.v2.Core.Radius;
using Multifactor.Radius.Adapter.v2.Tests.Fixture;

namespace Multifactor.Radius.Adapter.v2.Tests.Radius;

public class NasIdentifierParserTests
{
    [Fact]
    public void ParseNasIdentifier_ShouldParse()
    {
        var dictionary = TestUtils.GetRadiusDictionary();
        var packetService = new RadiusPacketService(NullLogger<RadiusPacketService>.Instance, dictionary);
        var secret = new SharedSecret("888");
        var packet = packetService.Parse(PacketExamples.DefaultAccessRequest, secret);

        packet.AddAttributeValue("NAS-Identifier", "Test-NAS-Identifier");

        var bytes = packetService.GetBytes(packet, secret);
        RadiusPacketNasIdentifierParser.TryParse(bytes, out var result);
        Assert.Equal("Test-NAS-Identifier", result);
    }
    
    [Fact]
    public void ParseNasIdentifier_NoAttribute_ShouldReturnNull()
    {
        var dictionary = TestUtils.GetRadiusDictionary();
        var packetService = new RadiusPacketService(NullLogger<RadiusPacketService>.Instance, dictionary);
        var secret = new SharedSecret("888");
        var packet = packetService.Parse(PacketExamples.DefaultAccessRequest, secret);

        var bytes = packetService.GetBytes(packet, secret);
        RadiusPacketNasIdentifierParser.TryParse(bytes, out var result);
        Assert.Null(result);
    }
}