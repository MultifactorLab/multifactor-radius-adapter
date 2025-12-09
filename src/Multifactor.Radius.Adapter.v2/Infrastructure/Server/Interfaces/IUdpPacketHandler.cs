using System.Net.Sockets;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Server.Interfaces;

public interface IUdpPacketHandler
{
    Task HandleAsync(UdpReceiveResult udpPacket);
}