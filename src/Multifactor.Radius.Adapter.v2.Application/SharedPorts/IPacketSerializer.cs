using Multifactor.Radius.Adapter.v2.Application.Core.Models;
using Multifactor.Radius.Adapter.v2.Application.Core.Models.Packet;

namespace Multifactor.Radius.Adapter.v2.Application.SharedPorts;

public interface IPacketSerializer
{
    byte[] Execute(RadiusPacket packet, SharedSecret sharedSecret);
}
