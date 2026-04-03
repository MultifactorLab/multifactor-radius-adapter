using Multifactor.Radius.Adapter.v2.Application.Core.Models;

namespace Multifactor.Radius.Adapter.v2.Application.SharedPorts;

public interface IPacketSerializer
{
    byte[] Execute(RadiusPacket packet, SharedSecret sharedSecret);
}
