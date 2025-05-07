using Multifactor.Radius.Adapter.v2.Core.Radius.Packet;
using Multifactor.Radius.Adapter.v2.Tests.Fixture;

namespace Multifactor.Radius.Adapter.v2.Tests.Radius;

public class RadiusPacketTests
{
    [Fact]
    public void CreateDefaultRadiusPacket_ShouldCreate()
    {
        var header = RadiusPacketHeader.Create(PacketCode.AccessRequest, 1);
        var authenticator = new RadiusAuthenticator();
        var requestAuthenticator = new RadiusAuthenticator();
        var packet = new RadiusPacket(header, authenticator, requestAuthenticator);
        Assert.Equal(header, packet.Header);
        Assert.Equal(authenticator, packet.Authenticator);
        Assert.Equal(requestAuthenticator, packet.RequestAuthenticator);
        Assert.Empty(packet.Attributes);
    }

    [Fact]
    public void AddPacketAttribute_ShouldAddSingleAttribute()
    {
        var header = RadiusPacketHeader.Create(PacketCode.AccessRequest, 1);
        var authenticator = new RadiusAuthenticator();
        var requestAuthenticator = new RadiusAuthenticator();
        var packet = new RadiusPacket(header, authenticator, requestAuthenticator);
        var attrName = "name";
        var attrValue = "value";
        packet.AddAttributeValue(attrName, attrValue);
        Assert.Single(packet.Attributes);
        Assert.Contains(attrName, packet.Attributes);
        var attribute = packet.Attributes[attrName];
        Assert.NotNull(attribute);
    }

    [Fact]
    public void AddPacketAttribute_ShouldAddTwoDifferentAttributes()
    {
        var header = RadiusPacketHeader.Create(PacketCode.AccessRequest, 1);
        var authenticator = new RadiusAuthenticator();
        var requestAuthenticator = new RadiusAuthenticator();
        var packet = new RadiusPacket(header, authenticator, requestAuthenticator);
        var attrName1 = "name1";
        var attrName2 = "name2";
        var attrValue = "value";
        packet.AddAttributeValue(attrName1, attrValue);
        packet.AddAttributeValue(attrName2, attrValue);
        Assert.Equal(2, packet.Attributes.Count);
        Assert.Contains(attrName1, packet.Attributes);
        Assert.Contains(attrName2, packet.Attributes);
        var attribute = packet.Attributes[attrName1];
        Assert.NotNull(attribute);
        attribute = packet.Attributes[attrName2];
        Assert.NotNull(attribute);
    }

    [Fact]
    public void AddPacketAttribute_ShouldAddSameAttributes()
    {
        var header = RadiusPacketHeader.Create(PacketCode.AccessRequest, 1);
        var authenticator = new RadiusAuthenticator();
        var requestAuthenticator = new RadiusAuthenticator();
        var packet = new RadiusPacket(header, authenticator, requestAuthenticator);
        var attrName = "name1";
        var attrValue = "value";
        packet.AddAttributeValue(attrName, attrValue);
        packet.AddAttributeValue(attrName, attrValue);

        Assert.Single(packet.Attributes);
        Assert.Contains(attrName, packet.Attributes);

        var attribute = packet.Attributes[attrName];
        Assert.NotNull(attribute);
        Assert.Equal(2, attribute.Values.Count);
    }

    [Fact]
    public void ReplaceAttribute_ShouldReplaceAttribute()
    {
        var header = RadiusPacketHeader.Create(PacketCode.AccessRequest, 1);
        var authenticator = new RadiusAuthenticator();
        var requestAuthenticator = new RadiusAuthenticator();
        var packet = new RadiusPacket(header, authenticator, requestAuthenticator);
        var attrName = "name1";
        var attrValue = "value";
        packet.AddAttributeValue(attrName, attrValue);

        var attribute = packet.Attributes[attrName];
        Assert.NotNull(attribute);
        Assert.Contains(attrValue, attribute.Values);

        var newValue = "newValue";
        packet.ReplaceAttribute(attrName, newValue);
        Assert.Single(packet.Attributes);

        attribute = packet.Attributes[attrName];
        Assert.NotNull(attribute);
        Assert.Contains(newValue, attribute.Values);
        Assert.DoesNotContain(attrValue, attribute.Values);
    }

    [Fact]
    public void RemoveAttribute_ShouldRemoveAttribute()
    {
        var header = RadiusPacketHeader.Create(PacketCode.AccessRequest, 1);
        var authenticator = new RadiusAuthenticator();
        var requestAuthenticator = new RadiusAuthenticator();
        var packet = new RadiusPacket(header, authenticator, requestAuthenticator);
        var attrName = "name1";
        var attrValue = "value";
        
        packet.AddAttributeValue(attrName, attrValue);
        Assert.Single(packet.Attributes);
        
        packet.RemoveAttribute(attrName);
        Assert.DoesNotContain(attrName, packet.Attributes);
    }

    [Theory]
    [ClassData(typeof(EmptyStringsListInput))]
    public void AddAttributeValue_EmptyName_ShouldThrow(string emptyString)
    {
        var header = RadiusPacketHeader.Create(PacketCode.AccessRequest, 1);
        var authenticator = new RadiusAuthenticator();
        var requestAuthenticator = new RadiusAuthenticator();
        var packet = new RadiusPacket(header, authenticator, requestAuthenticator);
        var attrName = emptyString;
        var attrValue = "value";
        
        Assert.Throws<ArgumentNullException>(() => packet.AddAttributeValue(attrName, attrValue)); 
    }
}