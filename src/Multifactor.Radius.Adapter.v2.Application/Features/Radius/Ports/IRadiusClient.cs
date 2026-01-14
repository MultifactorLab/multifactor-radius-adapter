using System.Net;

namespace Multifactor.Radius.Adapter.v2.Application.Features.Radius.Ports;

public interface IRadiusClient : IDisposable
{
    Task<byte[]?> SendPacketAsync(byte identifier, byte[] requestPacket, IPEndPoint remoteEndpoint, TimeSpan timeout);
}