using Multifactor.Radius.Adapter.v2.Application.Core.Models;

namespace Multifactor.Radius.Adapter.v2.Application.SharedPorts;

public interface IPacketParser
{
    RadiusPacket Execute(byte[]? packetBytes, SharedSecret sharedSecret, 
        RadiusAuthenticator? requestAuthenticator = null);
}