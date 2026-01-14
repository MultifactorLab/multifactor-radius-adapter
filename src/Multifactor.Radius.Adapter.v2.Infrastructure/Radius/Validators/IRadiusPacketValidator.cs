using Multifactor.Radius.Adapter.v2.Application.Features.Radius.Models;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Radius.Validators;

public interface IRadiusPacketValidator
{
    void ValidateRawPacket(byte[] packetBytes);
    void ValidateParsedPacket(RadiusPacket packet, SharedSecret sharedSecret);
    void ValidatePacketForSerialization(RadiusPacket packet);
}