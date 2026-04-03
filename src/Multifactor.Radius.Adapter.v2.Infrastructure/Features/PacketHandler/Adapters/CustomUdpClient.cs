using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using Multifactor.Core.Ldap.LangFeatures;
using Multifactor.Radius.Adapter.v2.Application.SharedPorts;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Features.PacketHandler.Adapters;

internal sealed class CustomUdpClient : IUdpClient
{
    private readonly UdpClient _udpClient;
    private readonly ILogger<CustomUdpClient> _logger;
    
    public CustomUdpClient(
        IPEndPoint endPoint,
        ILogger<CustomUdpClient> logger)
    {
        Throw.IfNull(endPoint, nameof(endPoint));
        _logger = logger;
        _udpClient = new UdpClient(endPoint);
        _logger.LogInformation("UDP client initialized on {Endpoint}", endPoint);
    }
    
    public async Task<UdpReceiveResult> ReceiveAsync(CancellationToken cancellationToken = default) 
        => await _udpClient.ReceiveAsync(cancellationToken);
    
    public async Task<int> SendAsync(
        byte[] datagram, 
        int bytesCount, 
        IPEndPoint endPoint)
    {
        ArgumentNullException.ThrowIfNull(datagram);

        if (bytesCount <= 0 || bytesCount > datagram.Length)
            throw new ArgumentOutOfRangeException(nameof(bytesCount));
            
        if (bytesCount > 4096)
        {
            _logger.LogWarning("Attempted to send oversized RADIUS packet: {Size} bytes", bytesCount);
            throw new ArgumentException($"RADIUS packet too large: {bytesCount} bytes");
        }
        
        try
        {
            await _udpClient.SendAsync(datagram, bytesCount, endPoint);
            return bytesCount;
        }
        catch (SocketException ex) when (ex.SocketErrorCode == SocketError.MessageSize)
        {
            _logger.LogWarning(ex, "Packet too large for MTU to {Endpoint}", endPoint);
            throw;
        }
        catch (SocketException ex) when (ex.SocketErrorCode == SocketError.HostUnreachable)
        {
            _logger.LogWarning(ex, "Host unreachable: {Endpoint}", endPoint);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UDP send error to {Endpoint}", endPoint);
            throw;
        }
    }
    
    public void Dispose()
    {
        try
        {
            _udpClient.Close();
            _udpClient.Dispose();
            _logger.LogDebug("UDP client disposed");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error disposing UDP client");
        }
    }
}
