using System.Net;
using System.Net.Sockets;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Server.Interfaces;

public interface IUdpClient : IDisposable
{
    Task<int> SendAsync(byte[] datagram, int bytesCount, IPEndPoint endPoint);
    Task<UdpReceiveResult> ReceiveAsync();
}