using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Multifactor.Radius.Adapter.EndToEndTests.Udp;

public class UdpSocket : IDisposable
{
    private readonly IPEndPoint _endPoint;
    private readonly Socket _socket;
    private const int MaxUdpSize = 65_535;
    
    public UdpSocket(IPAddress ip, int port)
    {
        _endPoint = new IPEndPoint(ip, port);
        _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        _socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.ReuseAddress, true);
    }
    
    public void Send(string data)
    {
        var bytes = Encoding.ASCII.GetBytes(data);
        Send(bytes);
    }
    
    public void Send(byte[] data)
    {
        _socket.SendTo(data, _endPoint);
    }

    public UdpData Receive()
    {
        var buffer = new byte[MaxUdpSize];
        var ep = (EndPoint)_endPoint;
        var received = _socket.ReceiveFrom(buffer, 0, buffer.Length, SocketFlags.None, ref ep);
        var m = new Memory<byte>(buffer, 0, received);
        return new UdpData(m);
    }
    
    public void Dispose()
    {
        _socket.Dispose();
    }
}