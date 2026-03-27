using Multifactor.Radius.Adapter.v2.Application.Core.Enum;
using Multifactor.Radius.Adapter.v2.Application.Core.Models;

namespace Multifactor.Radius.Adapter.v2.Application.Radius.Ports;

public interface IRadiusPacketService
{
    // Только высокоуровневые операции
    RadiusPacket ParsePacket(byte[] packetBytes, SharedSecret sharedSecret, 
        RadiusAuthenticator? requestAuthenticator = null);
    byte[] SerializePacket(RadiusPacket packet, SharedSecret sharedSecret);
    RadiusPacket CreateResponse(RadiusPacket request, PacketCode responseCode);
    bool TryGetNasIdentifier(byte[] packetBytes, out string nasIdentifier);
}