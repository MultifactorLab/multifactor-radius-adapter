using System.Net.Sockets;

namespace Multifactor.Radius.Adapter.v2.Server.Udp;

public interface IUdpPacketHandler
{
    Task HandleUdpPacket(UdpReceiveResult udpPacket);
}