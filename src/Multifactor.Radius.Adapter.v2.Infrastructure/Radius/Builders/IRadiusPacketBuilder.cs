using Multifactor.Radius.Adapter.v2.Application.Features.Radius.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.Radius.Models.Enums;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Radius.Builders;

public interface IRadiusPacketBuilder
{
    byte[] Build(RadiusPacket packet, SharedSecret sharedSecret);
    RadiusPacket CreateResponse(RadiusPacket request, PacketCode responseCode);
}