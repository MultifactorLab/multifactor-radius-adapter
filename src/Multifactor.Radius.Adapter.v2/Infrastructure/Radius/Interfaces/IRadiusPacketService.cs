using Multifactor.Radius.Adapter.v2.Domain.Radius;
using Multifactor.Radius.Adapter.v2.Domain.Radius.Packet;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Radius.Interfaces;

public interface IRadiusPacketService
{
    IRadiusPacket Parse(byte[] packetBytes, SharedSecret sharedSecret, RadiusAuthenticator requestAuthenticator = null);
    RadiusPacket CreateResponsePacket(IRadiusPacket radiusPacket, PacketCode responsePacketCode);
    byte[] GetBytes(IRadiusPacket packet, SharedSecret sharedSecret);
    bool TryGetNasIdentifier(byte[] packetBytes, out string nasIdentifier);
}