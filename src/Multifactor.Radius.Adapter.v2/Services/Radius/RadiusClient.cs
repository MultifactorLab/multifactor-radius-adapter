using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using Multifactor.Core.Ldap.LangFeatures;

namespace Multifactor.Radius.Adapter.v2.Services.Radius;

public sealed class RadiusClient : IDisposable
{
    private readonly UdpClient _udpClient;

    private readonly ConcurrentDictionary<Tuple<byte, IPEndPoint>, TaskCompletionSource<UdpReceiveResult>>
        _pendingRequests = new();

    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly ILogger _logger;

    /// <summary>
    /// Create a radius client which sends and receives responses on localEndpoint
    /// </summary>
    public RadiusClient(IPEndPoint localEndpoint, ILogger logger)
    {
        Throw.IfNull(localEndpoint);
        Throw.IfNull(logger);
        _logger = logger;
        _udpClient = new UdpClient(localEndpoint);

        _cancellationTokenSource = new CancellationTokenSource();

        var receiveTask = StartReceiveLoopAsync(_cancellationTokenSource.Token);
    }


    /// <summary>
    /// Send a packet with specified timeout
    /// </summary>
    public async Task<byte[]?> SendPacketAsync(byte identifier, byte[] requestPacket, IPEndPoint remoteEndpoint, TimeSpan timeout)
    {
        var responseTaskCs = new TaskCompletionSource<UdpReceiveResult>();

        if (_pendingRequests.TryAdd(new Tuple<byte, IPEndPoint>(identifier, remoteEndpoint), responseTaskCs))
        {
            await _udpClient.SendAsync(requestPacket, requestPacket.Length, remoteEndpoint);
            var completedTask = await Task.WhenAny(responseTaskCs.Task, Task.Delay(timeout));
            if (completedTask == responseTaskCs.Task)
            {
                return responseTaskCs.Task.Result.Buffer;
            }

            //timeout
            _logger.LogDebug("Server {remoteEndpoint:l} did not respond within {timeout:l}", remoteEndpoint, timeout);
            return null;
        }

        _logger.LogWarning("Network error");
        return null;
    }

    /// <summary>
    /// Receive packets in a loop and complete tasks based on identifier
    /// </summary>
    private async Task StartReceiveLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var response = await _udpClient.ReceiveAsync(cancellationToken);

                if (_pendingRequests.TryRemove(new Tuple<byte, IPEndPoint>(response.Buffer[1], response.RemoteEndPoint), out var taskCs))
                {
                    taskCs.SetResult(response);
                }
            }
            catch (ObjectDisposedException)
            {
                // This is thrown when udpclient is disposed, can be safely ignored
            }

            await Task.Delay(TimeSpan.FromMilliseconds(5), cancellationToken);
        }
    }

    public void Dispose()
    {
        _cancellationTokenSource.Cancel();
        _udpClient?.Close();
    }
}