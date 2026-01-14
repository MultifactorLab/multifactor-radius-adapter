using System.Collections.Concurrent;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using Multifactor.Radius.Adapter.v2.Application.Configuration;
using Multifactor.Radius.Adapter.v2.Application.Configuration.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.Radius.Ports;
using Multifactor.Radius.Adapter.v2.Application.Ports;

namespace Multifactor.Radius.Adapter.v2.Server;

public class AdapterServer : IAsyncDisposable
{
    private readonly IUdpClient _udpClient;
    private readonly IRadiusUdpAdapter _packetAdapter;
    private readonly ILogger<AdapterServer> _logger;
    private readonly ServiceConfiguration _serviceConfiguration;
    
    private Task? _receiveLoopTask;
    private CancellationTokenSource? _cts;
    private readonly SemaphoreSlim _concurrencyLimiter;
    private readonly ConcurrentBag<Task> _activeProcessingTasks = [];
    
    //Возможно сразу в конфигурацию вынести
    private const int ShoutDownTimeout = 30;
    private const int MaxConcurrentRequests = 1000;
    
    public AdapterServer(
        IUdpClient udpClient,
        IRadiusUdpAdapter packetAdapter,
        ServiceConfiguration serviceConfiguration,
        ILogger<AdapterServer> logger)
    {
        _udpClient = udpClient ?? throw new ArgumentNullException(nameof(udpClient));
        _packetAdapter = packetAdapter ?? throw new ArgumentNullException(nameof(packetAdapter));
        _serviceConfiguration = serviceConfiguration ?? throw new ArgumentNullException(nameof(serviceConfiguration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        _concurrencyLimiter = new SemaphoreSlim(MaxConcurrentRequests);
    }

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_receiveLoopTask != null)
        {
            _logger.LogWarning("Server is already running");
            return Task.CompletedTask;
        }

        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        
        LogStartupMessage();
        
        try
        {
            _receiveLoopTask = Task.Run(() => ReceiveLoopAsync(_cts.Token), _cts.Token);
            _logger.LogInformation("RADIUS server started successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start RADIUS server");
            throw;
        }

        return Task.CompletedTask;
    }

    private async Task ReceiveLoopAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Starting UDP receive loop");
        
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await _concurrencyLimiter.WaitAsync(cancellationToken);
                
                var packet = await _udpClient.ReceiveAsync(cancellationToken);
                
                var processingTask = ProcessPacketAsync(packet, cancellationToken);
                
                _activeProcessingTasks.Add(processingTask);
                
                _ = processingTask.ContinueWith(t => 
                {
                    _activeProcessingTasks.TryTake(out _);
                    _concurrencyLimiter.Release();
                }, TaskScheduler.Default);
                
                _logger.LogDebug("Received packet from {Host}:{Port}", 
                    packet.RemoteEndPoint.Address, packet.RemoteEndPoint.Port);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (SocketException ex) when (ex.SocketErrorCode == SocketError.Interrupted)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UDP receive loop");
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }
        }
        
        _logger.LogDebug("UDP receive loop stopped");
    }

    private async Task ProcessPacketAsync(UdpReceiveResult udpPacket, CancellationToken cancellationToken)
    {
        try
        {
            await _packetAdapter.Handle(udpPacket);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Failed to process packet from {Host}:{Port}", 
                udpPacket.RemoteEndPoint.Address, 
                udpPacket.RemoteEndPoint.Port);
        }
    }

    private async Task StopAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Stopping RADIUS server...");
        
        await _cts?.CancelAsync();
        
        if (_receiveLoopTask != null)
        {
            try
            {
                await _receiveLoopTask.WaitAsync(
                    TimeSpan.FromSeconds(ShoutDownTimeout), 
                    cancellationToken);
            }
            catch (TimeoutException)
            {
                _logger.LogWarning("Receive loop did not stop gracefully within timeout");
            }
            catch (OperationCanceledException)
            {
                // shoutdown was canceled
            }
        }
        
        if (!_activeProcessingTasks.IsEmpty)
        {
            _logger.LogDebug("Waiting for {Count} active processing tasks to complete", 
                _activeProcessingTasks.Count);
                
            try
            {
                await Task.WhenAll(_activeProcessingTasks)
                    .WaitAsync(TimeSpan.FromSeconds(ShoutDownTimeout), cancellationToken);
            }
            catch (TimeoutException)
            {
                _logger.LogWarning("Some processing tasks did not complete within timeout");
            }
        }
        
        _logger.LogInformation("RADIUS server stopped");
    }

    private void LogStartupMessage()
    {
        var endpoint = _serviceConfiguration.RootConfiguration.AdapterServerEndpoint;
        _logger.LogInformation(
            "Starting RADIUS server on {Host}:{Port} (Max concurrent: {MaxConcurrent})", 
            endpoint.Address, 
            endpoint.Port,
            MaxConcurrentRequests);
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
        
        _concurrencyLimiter?.Dispose();
        _cts?.Dispose();
        _udpClient?.Dispose();
        
        GC.SuppressFinalize(this);
    }
}
