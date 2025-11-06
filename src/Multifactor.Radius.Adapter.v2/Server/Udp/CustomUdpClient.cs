using System.Net;
using System.Net.Sockets;
using Multifactor.Core.Ldap.LangFeatures;

namespace Multifactor.Radius.Adapter.v2.Server.Udp;

public sealed class CustomUdpClient : IUdpClient
{
    private readonly UdpClient _udpClient;
    
    public CustomUdpClient(IPEndPoint endPoint)
    {
        Throw.IfNull(endPoint, nameof(endPoint));
        _udpClient = new UdpClient(endPoint);
    }
    
    public CustomUdpClient(string endPoint)
    {
        Throw.IfNullOrWhiteSpace(endPoint, nameof(endPoint));
        _udpClient = new UdpClient(IPEndPoint.Parse(endPoint));
    }
    
    public Task<UdpReceiveResult> ReceiveAsync() => _udpClient.ReceiveAsync();
    
    public Task<int> SendAsync(byte[] datagram, int bytesCount, IPEndPoint endPoint) => _udpClient.SendAsync(datagram, bytesCount, endPoint);
    
    public void Dispose()
    {
        _udpClient?.Close();
        _udpClient?.Dispose();
    }
}