using Multifactor.Radius.Adapter.v2.Core.Radius.Packet;

namespace Multifactor.Radius.Adapter.v2.EndToEndTests.Fixtures;

internal static class RadiusPacketFactory
{
    public static RadiusPacket AccessRequest(byte identifier = 0)
    {
        var header = RadiusPacketHeader.Create(PacketCode.AccessRequest, identifier);
        var packet = new RadiusPacket(header);
        return packet;
    }
    
    public static RadiusPacket AccessChallenge(byte identifier = 0)
    {
        var header = RadiusPacketHeader.Create(PacketCode.AccessChallenge, identifier);
        var packet = new RadiusPacket(header);
        return packet;
    }
    
    public static RadiusPacket AccessReject(byte identifier = 0)
    {
        var header = RadiusPacketHeader.Create(PacketCode.AccessReject, identifier);
        var packet = new RadiusPacket(header);
        return packet;
    }
    
    public static RadiusPacket StatusServer(byte identifier = 0)
    {
        var header = RadiusPacketHeader.Create(PacketCode.StatusServer, identifier);
        var packet = new RadiusPacket(header);
        return packet;
    }
}