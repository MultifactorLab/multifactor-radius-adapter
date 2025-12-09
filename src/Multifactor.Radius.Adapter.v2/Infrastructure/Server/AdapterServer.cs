using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using Multifactor.Radius.Adapter.v2.Domain;
using Multifactor.Radius.Adapter.v2.Domain.Radius.Attributes;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configuration.Service;
using Multifactor.Radius.Adapter.v2.Infrastructure.Server.Interfaces;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Server;

public class AdapterServer : IDisposable
{
    private readonly IUdpClient _udpClient;
    private readonly IUdpPacketHandler _packetHandler;
    private readonly ILogger<AdapterServer> _logger;
    private readonly IServiceConfiguration _serviceConfiguration;
    private readonly ApplicationVariables _applicationVariables;
    private readonly IRadiusDictionary _radiusDictionary;
    
    private bool _isRunning;
    
    public AdapterServer(
        IUdpClient udpClient,
        IUdpPacketHandler handler,
        IServiceConfiguration serviceConfiguration,
        ApplicationVariables applicationVariables,
        IRadiusDictionary radiusDictionary,
        ILogger<AdapterServer> logger)
    {
        _udpClient = udpClient;
        _packetHandler = handler;
        _serviceConfiguration = serviceConfiguration;
        _applicationVariables = applicationVariables;
        _radiusDictionary = radiusDictionary;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
            if (_isRunning)
            {
                _logger.LogInformation("Server is already running.");
                return;
            }

            _isRunning = true;
            LogHelloMessage();
            UdpReceiveResult udpPacket = new UdpReceiveResult();
            while (_isRunning && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var packet = await ReceivePackets();
                    udpPacket = packet;
                    _logger.LogInformation("Received packet from {host:l}:{port}.", packet.RemoteEndPoint.Address, packet.RemoteEndPoint.Port);
                    var task = Task.Factory.StartNew(() => ProcessPacket(packet), TaskCreationOptions.LongRunning);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error while processing packet from '{client:l}'", udpPacket.RemoteEndPoint.Address);
                }
            }
    }

    public Task Stop()
    {
        if (!_isRunning)
        {
            return Task.CompletedTask;
        }
        
        _logger.LogInformation("Stopping server");
        _isRunning = false;
        _udpClient?.Dispose();
        _logger.LogInformation("Server is stopped");
        return Task.CompletedTask;
    }

    private void LogHelloMessage()
    {
        _logger.LogInformation("Multifactor (c) cross-platform RADIUS Adapter, v. {Version:l}", _applicationVariables.AppVersion);
        _logger.LogInformation("Starting Radius server on {host:l}:{port}",
            _serviceConfiguration.ServiceServerEndpoint.Address,
            _serviceConfiguration.ServiceServerEndpoint.Port);

        _logger.LogInformation(_radiusDictionary.GetInfo());
    }

    private async Task ProcessPacket(UdpReceiveResult udpPacket)
    {
        try
        {
            await _packetHandler.HandleAsync(udpPacket);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process packet from {host:l}:{port}", udpPacket.RemoteEndPoint.Address, udpPacket.RemoteEndPoint.Port);
        }
        
        await Task.CompletedTask;
    }

    private Task<UdpReceiveResult> ReceivePackets()
    {
        return _udpClient.ReceiveAsync();
    }

    public void Dispose()
    {
        Stop();
    }
}