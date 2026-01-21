using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Logging;
using Multifactor.Radius.Adapter.v2.Application.Cache;
using Multifactor.Radius.Adapter.v2.Application.Configuration.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.Radius.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.Radius.Ports;
using Multifactor.Radius.Adapter.v2.Application.Features.Radius.Services;
using Multifactor.Radius.Adapter.v2.Shared.Extensions;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Adapters.PacketHandler;

public class RadiusUdpAdapter : IRadiusUdpAdapter
{
    private readonly ILogger<IRadiusUdpAdapter> _logger;
    private readonly ServiceConfiguration _serviceConfiguration;
    private readonly IRadiusPacketService _radiusPacketService;
    private readonly ICacheService _cache;
    private readonly IRadiusPacketProcessor _radiusPacketProcessor;

    public RadiusUdpAdapter(
        ServiceConfiguration serviceConfiguration,
        IRadiusPacketService packetService,
        ICacheService cache,
        IRadiusPacketProcessor radiusPacketProcessor,
        ILogger<IRadiusUdpAdapter> logger)
    {
        _serviceConfiguration = serviceConfiguration;
        _radiusPacketService = packetService;
        _logger = logger;
        _radiusPacketProcessor = radiusPacketProcessor;
        _cache = cache;
    }
    
    public async Task Handle(UdpReceiveResult udpPacket)
    {
        IPEndPoint? proxyEndpoint = null;
        var remoteEndpoint = udpPacket.RemoteEndPoint;
        var payload = udpPacket.Buffer;

        if (IsProxyProtocol(payload, out var sourceEndpoint, out var requestWithoutProxyHeader))
        {
            payload = requestWithoutProxyHeader;
            proxyEndpoint = remoteEndpoint;
            remoteEndpoint = sourceEndpoint;
        }
        
        var clientConfiguration = GetClientConfig(udpPacket);
        if (clientConfiguration == null)
        {
            _logger.LogWarning("Received packet from unknown client {host:l}:{port}, ignoring", remoteEndpoint.Address, remoteEndpoint.Port);
            return;
        } 
        
        var requestPacket = _radiusPacketService.ParsePacket(payload, new SharedSecret(clientConfiguration.RadiusSharedSecret));
        requestPacket.ProxyEndpoint = proxyEndpoint;
        requestPacket.RemoteEndpoint = remoteEndpoint;
        
        if (IsRetransmission(requestPacket))
        {
            _logger.LogDebug("Duplicated request from {host:l}:{port} id={id} client '{client:l}', ignoring", remoteEndpoint.Address, remoteEndpoint.Port, requestPacket.Identifier, clientConfiguration.Name);
            return;
        }
        
        await _radiusPacketProcessor.ProcessPacketAsync(requestPacket, clientConfiguration);
    }

    private static bool IsProxyProtocol(byte[] payload, out IPEndPoint sourceEndpoint, out byte[] requestWithoutProxyHeader)
    {
        //https://www.haproxy.org/download/1.8/doc/proxy-protocol.txt

        sourceEndpoint = null;
        requestWithoutProxyHeader = null;

        if (payload.Length < 6)
            return false;

        var proxySig = Encoding.ASCII.GetString(payload.Take(5).ToArray());

        if (proxySig != "PROXY")
            return false;

        var lf = Array.IndexOf(payload, (byte)'\n');
        var headerBytes = payload.Take(lf + 1).ToArray();
        var header = Encoding.ASCII.GetString(headerBytes);

        var parts = header.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        var sourceIp = parts[2];
        var sourcePort = int.Parse(parts[4]);

        sourceEndpoint = new IPEndPoint(IPAddress.Parse(sourceIp), sourcePort);
        requestWithoutProxyHeader = payload.Skip(lf + 1).ToArray();

        return true;
    }

    private ClientConfiguration? GetClientConfig(UdpReceiveResult udpPacket)
    {
        ClientConfiguration? clientConfiguration = null;
        if (_radiusPacketService.TryGetNasIdentifier(udpPacket.Buffer, out var nasIdentifier))
            clientConfiguration = _serviceConfiguration.GetClientConfiguration(nasIdentifier);
        clientConfiguration ??= _serviceConfiguration.GetClientConfiguration(udpPacket.RemoteEndPoint.Address);

        return clientConfiguration;
    }

    private bool IsRetransmission(RadiusPacket requestPacket)
    {
        var packetKey = CreateUniquePacketKey(requestPacket);
        if (_cache.TryGetValue<object>(packetKey, out _))
            return true;

        _cache.Set(packetKey, 1, DateTimeOffset.UtcNow.AddMinutes(1));

        return false;
    }

    private static string CreateUniquePacketKey(RadiusPacket requestPacket)
    {
        var base64Authenticator = requestPacket.Authenticator.Value.ToBase64();
        return $"{requestPacket.Code:d}:{requestPacket.Identifier}:{requestPacket.RemoteEndpoint}:{requestPacket.UserName}:{base64Authenticator}";
    }
}