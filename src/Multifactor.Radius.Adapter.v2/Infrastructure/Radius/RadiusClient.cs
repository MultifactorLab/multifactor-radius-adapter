using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using Multifactor.Radius.Adapter.v2.Infrastructure.Radius.Interfaces;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Radius;

public sealed class RadiusClient : IRadiusClient
{
    private readonly UdpClient _udpClient;
    private readonly ConcurrentDictionary<RequestKey, TaskCompletionSource<UdpReceiveResult>> _pendingRequests = new();
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly ILogger<RadiusClient> _logger;

    public RadiusClient(IPEndPoint localEndpoint, ILogger<RadiusClient> logger)
    {
        ArgumentNullException.ThrowIfNull(localEndpoint);
        ArgumentNullException.ThrowIfNull(logger);
        
        _logger = logger;
        _udpClient = new UdpClient(localEndpoint);
        _cancellationTokenSource = new CancellationTokenSource();

        _ = StartReceiveLoopAsync(_cancellationTokenSource.Token);
    }

    public async Task<byte[]?> SendPacketAsync(byte identifier, byte[] requestPacket, IPEndPoint remoteEndpoint, TimeSpan timeout)
    {
        var requestKey = new RequestKey(identifier, remoteEndpoint);
        var responseTcs = new TaskCompletionSource<UdpReceiveResult>();

        if (!_pendingRequests.TryAdd(requestKey, responseTcs))
        {
            _logger.LogWarning("Failed to add pending request for identifier {Identifier}", identifier);
            return null;
        }

        try
        {
            await _udpClient.SendAsync(requestPacket, requestPacket.Length, remoteEndpoint);
            
            var completedTask = await Task.WhenAny(
                responseTcs.Task, 
                Task.Delay(timeout, _cancellationTokenSource.Token));

            if (completedTask == responseTcs.Task)
                return responseTcs.Task.Result.Buffer;

            _logger.LogDebug("Timeout waiting for response from {RemoteEndpoint}", remoteEndpoint);
            return null;
        }
        finally
        {
            _pendingRequests.TryRemove(requestKey, out _);
        }
    }

    private async Task StartReceiveLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var response = await _udpClient.ReceiveAsync(cancellationToken);
                var identifier = response.Buffer[1];
                var requestKey = new RequestKey(identifier, response.RemoteEndPoint);

                if (_pendingRequests.TryRemove(requestKey, out var tcs))
                    tcs.TrySetResult(response);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in receive loop");
            }

            await Task.Delay(TimeSpan.FromMilliseconds(1), cancellationToken);
        }
    }

    public void Dispose()
    {
        _cancellationTokenSource.Cancel();
        _udpClient?.Dispose();
        _cancellationTokenSource.Dispose();
        
        foreach (var request in _pendingRequests)
            request.Value.TrySetCanceled();
    }

    private readonly record struct RequestKey(byte Identifier, IPEndPoint RemoteEndpoint);
}