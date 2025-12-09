using System.Net;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Radius.Interfaces;

public interface IRadiusClient : IDisposable
{
    Task<byte[]?> SendPacketAsync(byte identifier, byte[] requestPacket, IPEndPoint remoteEndpoint, TimeSpan timeout);
}