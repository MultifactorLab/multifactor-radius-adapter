using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using Multifactor.Core.Ldap.LangFeatures;
using Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.FirstFactor.Ports;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Radius_remove_.Client;

internal sealed class RadiusClient : IRadiusClient
{
    private readonly UdpClient _udpClient; 
    private readonly ConcurrentDictionary<string, PendingRequest> _pendingRequests = new();
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly ILogger<RadiusClient> _logger;
    private readonly Timer _cleanupTimer;
    private readonly TimeSpan _cleanupInterval = TimeSpan.FromMinutes(1);
    
    private class PendingRequest //todo
    {
        public TaskCompletionSource<byte[]?> TaskCompletionSource { get; }
        public DateTime CreatedAt { get; }
        public byte Identifier { get; }
        public IPEndPoint RemoteEndpoint { get; }
        
        public PendingRequest(byte identifier, IPEndPoint remoteEndpoint)
        {
            TaskCompletionSource = new TaskCompletionSource<byte[]?>();
            CreatedAt = DateTime.UtcNow;
            Identifier = identifier;
            RemoteEndpoint = remoteEndpoint;
        }
    }

    /// <summary>
    /// Create a radius client which sends and receives responses on localEndpoint
    /// </summary>
    public RadiusClient(IPEndPoint localEndpoint, ILogger<RadiusClient> logger)
    {
        Throw.IfNull(localEndpoint);
        Throw.IfNull(logger);
        
        _logger = logger;
        _udpClient = new UdpClient(localEndpoint);
        _cancellationTokenSource = new CancellationTokenSource();

        // Запускаем периодическую очистку старых запросов
        _cleanupTimer = new Timer(CleanupOldRequests, null, _cleanupInterval, _cleanupInterval);

        // Запускаем цикл приема пакетов
        _ = StartReceiveLoopAsync(_cancellationTokenSource.Token);
    }

    /// <summary>
    /// Send a packet with specified timeout
    /// </summary>
    public async Task<byte[]?> SendPacketAsync(byte identifier, byte[] requestPacket, IPEndPoint remoteEndpoint, TimeSpan timeout)
    {
        var key = CreateRequestKey(identifier, remoteEndpoint);
        var pendingRequest = new PendingRequest(identifier, remoteEndpoint);
        
        var timeoutCancellation = new CancellationTokenSource(timeout);
        timeoutCancellation.Token.Register(() =>
        {
            if (_pendingRequests.TryRemove(key, out var request))
            {
                request.TaskCompletionSource.TrySetCanceled();
                _logger.LogDebug("Request timeout for identifier {identifier} to {remoteEndpoint}", 
                    identifier, remoteEndpoint);
            }
        }, useSynchronizationContext: false);

        try
        {
            if (_pendingRequests.TryAdd(key, pendingRequest))
            {
                await _udpClient.SendAsync(requestPacket, remoteEndpoint, timeoutCancellation.Token);
                return await pendingRequest.TaskCompletionSource.Task;
            }
            else
            {
                _logger.LogWarning("Duplicate request detected for identifier {identifier} to {remoteEndpoint}", 
                    identifier, remoteEndpoint);
                return null;
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Error sending packet to {remoteEndpoint}", remoteEndpoint);
            _pendingRequests.TryRemove(key, out _);
            pendingRequest.TaskCompletionSource.TrySetException(ex);
            return null;
        }
        finally
        {
            timeoutCancellation.Dispose();
            
            _pendingRequests.TryRemove(key, out _);
        }
    }

    /// <summary>
    /// Receive packets in a loop and complete tasks based on identifier
    /// </summary>
    private async Task StartReceiveLoopAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Starting receive loop");
        
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var response = await _udpClient.ReceiveAsync(cancellationToken);
                ProcessReceivedPacket(response);
            }
            catch (ObjectDisposedException)
            {
                // This is thrown when udpclient is disposed, can be safely ignored
                break;
            }
            catch (OperationCanceledException)
            {
                // Cancellation requested
                break;
            }
            catch (SocketException ex)
            {
                _logger.LogError(ex, "Socket error in receive loop");
                await Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in receive loop");
                await Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken);
            }
        }
        
        _logger.LogDebug("Receive loop stopped");
    }

    /// <summary>
    /// Process received UDP packet
    /// </summary>
    private void ProcessReceivedPacket(UdpReceiveResult result)
    {
        try
        {
            if (result.Buffer.Length < 2)
            {
                _logger.LogDebug("Received packet too small: {length} bytes", result.Buffer.Length);
                return;
            }

            var identifier = result.Buffer[1];
            var key = CreateRequestKey(identifier, result.RemoteEndPoint);

            if (_pendingRequests.TryRemove(key, out var pendingRequest))
            {
                pendingRequest.TaskCompletionSource.TrySetResult(result.Buffer);
                _logger.LogDebug("Received response for identifier {identifier} from {remoteEndpoint}", 
                    identifier, result.RemoteEndPoint);
            }
            else
            {
                _logger.LogDebug("Received unexpected response for identifier {identifier} from {remoteEndpoint}", 
                    identifier, result.RemoteEndPoint);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing received packet from {remoteEndpoint}", 
                result.RemoteEndPoint);
        }
    }

    /// <summary>
    /// Cleanup old pending requests that haven't received responses
    /// </summary>
    private void CleanupOldRequests(object? state)
    {
        try
        {
            var cutoffTime = DateTime.UtcNow - TimeSpan.FromMinutes(5);
            var removedCount = 0;
            
            foreach (var kvp in _pendingRequests)
            {
                if (kvp.Value.CreatedAt >= cutoffTime) continue;
                if (!_pendingRequests.TryRemove(kvp.Key, out var request)) continue;
                request.TaskCompletionSource.TrySetCanceled();
                removedCount++;
            }
            
            if (removedCount > 0)
            {
                _logger.LogDebug("Cleaned up {count} old pending requests", removedCount);
            }
            
            _logger.LogTrace("Pending requests count: {count}", _pendingRequests.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during cleanup of pending requests");
        }
    }

    /// <summary>
    /// Create a unique key for a request
    /// </summary>
    private static string CreateRequestKey(byte identifier, IPEndPoint remoteEndpoint)
    {
        return $"{identifier}_{remoteEndpoint.Address}:{remoteEndpoint.Port}";
    }

    public void Dispose()
    {
        try
        {
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource?.Dispose();
            
            _cleanupTimer.Change(Timeout.Infinite, Timeout.Infinite);
            _cleanupTimer.Dispose();
            
            foreach (var kvp in _pendingRequests)
            {
                kvp.Value.TaskCompletionSource.TrySetCanceled();
            }
            _pendingRequests.Clear();
            
            _udpClient.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during disposal");
        }
        
        _logger.LogDebug("RadiusClient disposed");
    }
}