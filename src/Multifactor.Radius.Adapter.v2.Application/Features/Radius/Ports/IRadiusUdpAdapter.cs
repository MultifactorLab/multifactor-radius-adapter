using System.Net.Sockets;

namespace Multifactor.Radius.Adapter.v2.Application.Features.Radius.Ports;

public interface IRadiusUdpAdapter
{
    Task Handle(UdpReceiveResult udpPacket);
}