using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using Multifactor.Core.Ldap.LangFeatures;
using Multifactor.Radius.Adapter.v2.Server.Udp;

namespace Multifactor.Radius.Adapter.v2.Server;

public class AdapterServer : IDisposable
{
    private readonly IUdpClient _udpClient;
    private readonly IUdpPacketHandler _packetHandler;
    private readonly ILogger<AdapterServer> _logger;
    
    private bool _isRunning;
    
    public AdapterServer(IUdpClient udpClient, IUdpPacketHandler handler, ILogger<AdapterServer> logger)
    {
        Throw.IfNull(udpClient);
        Throw.IfNull(handler);
        Throw.IfNull(logger);
        
        _udpClient = udpClient;
        _packetHandler = handler;
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
            _logger.LogInformation("Server is started.");
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

    private async Task ProcessPacket(UdpReceiveResult udpPacket)
    {
        try
        {
            await _packetHandler.HandleUdpPacket(udpPacket);
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