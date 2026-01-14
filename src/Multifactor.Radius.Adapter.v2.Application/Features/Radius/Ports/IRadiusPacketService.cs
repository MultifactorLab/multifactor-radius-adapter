using Multifactor.Radius.Adapter.v2.Application.Features.Radius.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.Radius.Models.Enums;

namespace Multifactor.Radius.Adapter.v2.Application.Features.Radius.Ports;

public interface IRadiusPacketService
{
    // Только высокоуровневые операции
    RadiusPacket ParsePacket(byte[] packetBytes, SharedSecret sharedSecret, RadiusAuthenticator? requestAuthenticator = null);
    byte[] SerializePacket(RadiusPacket packet, SharedSecret sharedSecret);
    RadiusPacket CreateResponse(RadiusPacket request, PacketCode responseCode);
    bool TryGetNasIdentifier(byte[] packetBytes, out string nasIdentifier);
}