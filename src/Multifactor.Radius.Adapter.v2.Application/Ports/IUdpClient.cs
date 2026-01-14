using System.Net;
using System.Net.Sockets;

namespace Multifactor.Radius.Adapter.v2.Application.Ports;

public interface IUdpClient : IDisposable
{
    Task<int> SendAsync(byte[] datagram, int bytesCount, IPEndPoint endPoint, 
        CancellationToken cancellationToken = default);
    Task<UdpReceiveResult> ReceiveAsync(CancellationToken cancellationToken = default);
}