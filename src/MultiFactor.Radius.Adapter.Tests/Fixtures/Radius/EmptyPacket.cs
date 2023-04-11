using MultiFactor.Radius.Adapter.Core.Radius;

namespace MultiFactor.Radius.Adapter.Tests.Fixtures.Radius;

internal static class RadiusPacketFactory
{
    public static IRadiusPacket AccessRequest(Action<IRadiusPacket>? configurePacket = null)
    {
        var packet = new RadiusPacket(PacketCode.AccessRequest, 0, "secret");
        configurePacket?.Invoke(packet);
        return packet;
    }
    
    public static IRadiusPacket AccessChallenge(Action<IRadiusPacket>? configurePacket = null)
    {
        var packet = new RadiusPacket(PacketCode.AccessChallenge, 0, "secret");
        configurePacket?.Invoke(packet);
        return packet;
    }
    
    public static IRadiusPacket AccessReject(Action<IRadiusPacket>? configurePacket = null)
    {
        var packet = new RadiusPacket(PacketCode.AccessReject, 0, "secret");
        configurePacket?.Invoke(packet);
        return packet;
    }
    
    public static IRadiusPacket StatusServer(Action<IRadiusPacket>? configurePacket = null)
    {
        var packet = new RadiusPacket(PacketCode.StatusServer, 0, "secret");
        configurePacket?.Invoke(packet);
        return packet;
    }
}