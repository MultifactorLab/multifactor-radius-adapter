using System.Collections.Concurrent;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using Multifactor.Radius.Adapter.v2.Application.Core.Models;
using Multifactor.Radius.Adapter.v2.Application.SharedPorts;
using Multifactor.Radius.Adapter.v2.Features.PacketHandle;

namespace Multifactor.Radius.Adapter.v2.Server;

internal sealed class AdapterServer : IAsyncDisposable
{
    private readonly IUdpClient _udpClient;
    private readonly IRadiusUdpAdapter _packetAdapter;
    private readonly ApplicationVariables _applicationVariables;
    private readonly ServiceConfiguration _serviceConfiguration;
    private readonly ILogger<AdapterServer> _logger;
    
    private Task? _receiveLoopTask;
    private CancellationTokenSource? _cts;
    private readonly SemaphoreSlim _concurrencyLimiter;
    private readonly ConcurrentBag<Task> _activeProcessingTasks = [];
    
    private const int ShoutDownTimeout = 30;
    private const int MaxConcurrentRequests = 10000;
    
    public AdapterServer(
        IUdpClient udpClient,
        IRadiusUdpAdapter packetAdapter,
        ApplicationVariables applicationVariables,
        ServiceConfiguration serviceConfiguration,
        ILogger<AdapterServer> logger)
    {
        _udpClient = udpClient;
        _packetAdapter = packetAdapter;
        _applicationVariables = applicationVariables;
        _serviceConfiguration = serviceConfiguration;
        _logger = logger;
        
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
        
        if(_cts != null)
            await _cts.CancelAsync();
        
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
        _logger.LogInformation("Multifactor (c) cross-platform RADIUS Adapter, v. {Version:l}", _applicationVariables.AppVersion);
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
        
        _concurrencyLimiter.Dispose();
        _cts?.Dispose();
        _udpClient.Dispose();
        
        GC.SuppressFinalize(this);
    }
}
