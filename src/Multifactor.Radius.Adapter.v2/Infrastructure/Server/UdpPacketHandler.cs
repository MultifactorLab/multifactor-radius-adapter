using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Logging;
using Multifactor.Radius.Adapter.v2.Domain.Radius;
using Multifactor.Radius.Adapter.v2.Domain.Radius.Packet;
using Multifactor.Radius.Adapter.v2.Infrastructure.Cache;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configuration.Client;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configuration.Service;
using Multifactor.Radius.Adapter.v2.Infrastructure.Radius.Interfaces;
using Multifactor.Radius.Adapter.v2.Infrastructure.Server.Interfaces;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Server;

public class UdpPacketHandler : IUdpPacketHandler
{
    private readonly IServiceConfiguration _serviceConfig;
    private readonly IRadiusPacketService _packetService;
    private readonly IRadiusPacketProcessor _packetProcessor;
    private readonly ICacheService _cache;
    private readonly ILogger<UdpPacketHandler> _logger;

    public UdpPacketHandler(
        IServiceConfiguration serviceConfig,
        IRadiusPacketService packetService,
        IRadiusPacketProcessor packetProcessor,
        ICacheService cache,
        ILogger<UdpPacketHandler> logger)
    {
        _serviceConfig = serviceConfig ?? throw new ArgumentNullException(nameof(serviceConfig));
        _packetService = packetService ?? throw new ArgumentNullException(nameof(packetService));
        _packetProcessor = packetProcessor ?? throw new ArgumentNullException(nameof(packetProcessor));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    public async Task HandleAsync(UdpReceiveResult udpPacket)
    {
        var (payload, remoteEndpoint, proxyEndpoint) = ParseProxyProtocol(udpPacket);
        
        var clientConfig = FindClientConfiguration(udpPacket.Buffer, remoteEndpoint);
        if (clientConfig == null)
        {
            LogUnknownClient(remoteEndpoint);
            return;
        } 
        
        var requestPacket = ParseRequestPacket(payload, remoteEndpoint, proxyEndpoint, clientConfig);
        
        if (IsDuplicateRequest(requestPacket))
        {
            LogDuplicateRequest(requestPacket, clientConfig);
            return;
        }
        
        await ProcessPacketAsync(requestPacket, clientConfig);
    }

    private (byte[] Payload, IPEndPoint RemoteEndpoint, IPEndPoint? ProxyEndpoint) ParseProxyProtocol(UdpReceiveResult udpPacket)
    {
        var payload = udpPacket.Buffer;
        var remoteEndpoint = udpPacket.RemoteEndPoint;
        IPEndPoint? proxyEndpoint = null;

        if (TryParseProxyProtocol(payload, out var sourceEndpoint, out var cleanPayload))
        {
            payload = cleanPayload;
            proxyEndpoint = remoteEndpoint;
            remoteEndpoint = sourceEndpoint;
        }

        return (payload, remoteEndpoint, proxyEndpoint);
    }

    private bool TryParseProxyProtocol(byte[] payload, out IPEndPoint sourceEndpoint, out byte[] cleanPayload)
    {
        sourceEndpoint = null;
        cleanPayload = null;

        if (payload.Length < 6)
            return false;

        var proxySignature = Encoding.ASCII.GetString(payload, 0, 5);
        if (proxySignature != "PROXY")
            return false;

        var newLineIndex = Array.IndexOf(payload, (byte)'\n');
        if (newLineIndex == -1)
            return false;

        var header = Encoding.ASCII.GetString(payload, 0, newLineIndex);
        var parts = header.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length < 6)
            return false;

        if (!IPAddress.TryParse(parts[2], out var sourceIp) || !int.TryParse(parts[4], out var sourcePort))
            return false;

        sourceEndpoint = new IPEndPoint(sourceIp, sourcePort);
        cleanPayload = new byte[payload.Length - newLineIndex - 1];
        Buffer.BlockCopy(payload, newLineIndex + 1, cleanPayload, 0, cleanPayload.Length);

        return true;
    }

    private IClientConfiguration? FindClientConfiguration(byte[] packetBytes, IPEndPoint remoteEndpoint)
    {
        if (_packetService.TryGetNasIdentifier(packetBytes, out var nasIdentifier))
        {
            var config = _serviceConfig.GetClient(nasIdentifier);
            if (config != null)
                return config;
        }

        return _serviceConfig.GetClient(remoteEndpoint.Address);
    }

    private IRadiusPacket ParseRequestPacket(byte[] payload, IPEndPoint remoteEndpoint, IPEndPoint? proxyEndpoint, IClientConfiguration clientConfig)
    {
        var secret = new SharedSecret(clientConfig.RadiusSharedSecret);
        var packet = _packetService.Parse(payload, secret);
        
        packet.RemoteEndpoint = remoteEndpoint;
        packet.ProxyEndpoint = proxyEndpoint;
        
        return packet;
    }

    private bool IsDuplicateRequest(IRadiusPacket requestPacket)
    {
        var cacheKey = CreateCacheKey(requestPacket);
        
        if (_cache.TryGetValue<object>(cacheKey, out var cachedPacket))
            return true;

        _cache.Set(cacheKey, 1);
        return false;
    }

    private string CreateCacheKey(IRadiusPacket packet)
    {
        var authenticatorBase64 = Convert.ToBase64String(packet.Authenticator.Value);
        return $"{packet.Code}:{packet.Identifier}:{packet.RemoteEndpoint}:{packet.UserName}:{authenticatorBase64}";
    }

    private async Task ProcessPacketAsync(IRadiusPacket requestPacket, IClientConfiguration clientConfig)
    {
        try
        {
            await _packetProcessor.ProcessAsync(requestPacket, clientConfig);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing packet from {RemoteEndpoint}", requestPacket.RemoteEndpoint);
        }
    }

    private void LogUnknownClient(IPEndPoint endpoint)
    {
        _logger.LogWarning("Unknown client {Host}:{Port}, ignoring", endpoint.Address, endpoint.Port);
    }

    private void LogDuplicateRequest(IRadiusPacket packet, IClientConfiguration config)
    {
        _logger.LogDebug("Duplicate request from {Host}:{Port} id={Id} client='{Client}', ignoring", 
            packet.RemoteEndpoint.Address, 
            packet.RemoteEndpoint.Port, 
            packet.Identifier, 
            config.Name);
    }
}