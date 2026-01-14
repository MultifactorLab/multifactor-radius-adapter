using Multifactor.Radius.Adapter.v2.Application.Features.Radius.Models;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Radius.Parsers;

public interface IRadiusPacketParser
{
    RadiusPacket Parse(byte[] packetBytes, SharedSecret sharedSecret);
    RadiusPacket Parse(byte[] packetBytes, SharedSecret sharedSecret, RadiusAuthenticator requestAuthenticator);
}