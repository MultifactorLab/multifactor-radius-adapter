using Multifactor.Radius.Adapter.v2.Core.Radius;
using Multifactor.Radius.Adapter.v2.Core.Radius.Packet;

namespace Multifactor.Radius.Adapter.v2.Services.Radius;

public interface IRadiusPacketService
{
    RadiusPacket Parse(byte[] packetBytes, SharedSecret sharedSecret, RadiusAuthenticator requestAuthenticator = null);
    RadiusPacket CreateResponsePacket(IRadiusPacket radiusPacket, PacketCode responsePacketCode);
    byte[] GetBytes(IRadiusPacket packet, SharedSecret sharedSecret);
}