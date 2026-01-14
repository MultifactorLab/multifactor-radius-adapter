using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Multifactor.Core.Ldap.LangFeatures;
using Multifactor.Radius.Adapter.v2.Application.Ports;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Adapters.Udp;

public sealed class CustomUdpClient : IUdpClient
{
    private readonly UdpClient _udpClient;
    private readonly ILogger<CustomUdpClient> _logger;
    private readonly UdpClientOptions _options;
    
    public CustomUdpClient(
        IPEndPoint endPoint,
        ILogger<CustomUdpClient> logger,
        IOptions<UdpClientOptions> options)
    {
        Throw.IfNull(endPoint, nameof(endPoint));
        
        _logger = logger;
        _options = options?.Value ?? new UdpClientOptions();
        
        _udpClient = new UdpClient();
        ConfigureSocket(endPoint);
        
        _logger?.LogInformation("UDP client initialized on {Endpoint}", endPoint);
    }
    
    private void ConfigureSocket(IPEndPoint endPoint)
    {
        var socket = _udpClient.Client;
        
        socket.ReceiveBufferSize = _options.ReceiveBufferSize;
        socket.SendBufferSize = _options.SendBufferSize;
        
        socket.ReceiveTimeout = _options.ReceiveTimeoutMs;
        socket.Ttl = _options.Ttl;
        socket.DontFragment = true;
        
        socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        
        socket.Bind(endPoint);
        
        socket.NoDelay = true;
    }
    
    public async Task<UdpReceiveResult> ReceiveAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _udpClient.ReceiveAsync(cancellationToken);
        }
        catch (SocketException ex) when (ex.SocketErrorCode == SocketError.Interrupted)
        {
            throw new OperationCanceledException("UDP receive was interrupted", ex, cancellationToken);
        }
        catch (ObjectDisposedException) when (cancellationToken.IsCancellationRequested)
        {
            throw new OperationCanceledException(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "UDP receive error");
            throw;
        }
    }
    
    public async Task<int> SendAsync(
        byte[] datagram, 
        int bytesCount, 
        IPEndPoint endPoint,
        CancellationToken cancellationToken = default)
    {
        if (datagram == null)
            throw new ArgumentNullException(nameof(datagram));
            
        if (bytesCount <= 0 || bytesCount > datagram.Length)
            throw new ArgumentOutOfRangeException(nameof(bytesCount));
            
        if (bytesCount > 4096)
        {
            _logger?.LogWarning("Attempted to send oversized RADIUS packet: {Size} bytes", bytesCount);
            throw new ArgumentException($"RADIUS packet too large: {bytesCount} bytes");
        }
        
        try
        {
            await _udpClient.SendAsync(datagram, bytesCount, endPoint); //TODO разобраться async хочется
            return bytesCount;
        }
        catch (SocketException ex) when (ex.SocketErrorCode == SocketError.MessageSize)
        {
            _logger?.LogWarning(ex, "Packet too large for MTU to {Endpoint}", endPoint);
            throw;
        }
        catch (SocketException ex) when (ex.SocketErrorCode == SocketError.HostUnreachable)
        {
            _logger?.LogWarning(ex, "Host unreachable: {Endpoint}", endPoint);
            throw;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "UDP send error to {Endpoint}", endPoint);
            throw;
        }
    }
    
    public void Dispose()
    {
        try
        {
            _udpClient?.Close();
            _udpClient?.Dispose();
            _logger?.LogDebug("UDP client disposed");
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Error disposing UDP client");
        }
    }
}

public class UdpClientOptions
{
    public int ReceiveBufferSize { get; set; } = 64 * 1024; // 64KB
    public int SendBufferSize { get; set; } = 64 * 1024;    // 64KB
    public int ReceiveTimeoutMs { get; set; } = 0;
    public short Ttl { get; set; } = 32;
    public bool EnableBroadcast { get; set; } = false;
}