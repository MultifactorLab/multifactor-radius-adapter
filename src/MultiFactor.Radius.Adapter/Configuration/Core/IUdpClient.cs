using System.Net.Sockets;
using System.Net;
using System.Threading.Tasks;

namespace MultiFactor.Radius.Adapter.Configuration.Core;

public interface IUdpClient
{
    Task<UdpReceiveResult> ReceiveAsync();
    int Send(byte[] dgram, int bytes, IPEndPoint endPoint);
    void Close();
}
