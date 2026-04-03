using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Logging;
using Multifactor.Radius.Adapter.v2.Application.Core.Models;
using Multifactor.Radius.Adapter.v2.Application.Core.Models.Abstractions;
using Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.Ports;
using Multifactor.Radius.Adapter.v2.Application.SharedPorts;

namespace Multifactor.Radius.Adapter.v2.Features.PacketHandle;

internal interface IRadiusUdpAdapter
{
    Task Handle(UdpReceiveResult udpPacket);
}

internal sealed class RadiusUdpAdapter : IRadiusUdpAdapter
{
    private readonly ILogger<IRadiusUdpAdapter> _logger;
    private readonly ServiceConfiguration _serviceConfiguration;
    private readonly IPacketParser _packetParser;
    private readonly IPacketKeyCache _cache;
    private readonly IRadiusPacketProcessor _radiusPacketProcessor;

    public RadiusUdpAdapter(
        ServiceConfiguration serviceConfiguration,
        IRadiusPacketProcessor radiusPacketProcessor,
        IPacketKeyCache cache,
        ILogger<IRadiusUdpAdapter> logger, IPacketParser packetParser)
    {
        _serviceConfiguration = serviceConfiguration;
        _radiusPacketProcessor = radiusPacketProcessor;
        _cache = cache;
        _logger = logger;
        _packetParser = packetParser;
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
            _logger.LogWarning("Received packet from unknown client {host:l}:{port}, ignoring", remoteEndpoint?.Address, remoteEndpoint?.Port);
            return;
        } 
        
        var requestPacket = _packetParser.Execute(payload, new SharedSecret(clientConfiguration.RadiusSharedSecret));
        requestPacket.ProxyEndpoint = proxyEndpoint;
        requestPacket.RemoteEndpoint = remoteEndpoint;
        
        if (IsRetransmission(requestPacket))
        {
            _logger.LogDebug("Duplicated request from {host:l}:{port} id={id} client '{client:l}', ignoring", remoteEndpoint?.Address, remoteEndpoint?.Port, requestPacket.Identifier, clientConfiguration.Name);
            return;
        }
        
        await _radiusPacketProcessor.Execute(requestPacket, clientConfiguration);
    }

    ///https://www.haproxy.org/download/1.8/doc/proxy-protocol.txt
    private static bool IsProxyProtocol(byte[] payload, out IPEndPoint? sourceEndpoint, out byte[]? requestWithoutProxyHeader)
    {
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

    private IClientConfiguration? GetClientConfig(UdpReceiveResult udpPacket)
    {
        IClientConfiguration? clientConfiguration = null;
        if (udpPacket.Buffer is null)
        {
            throw new ArgumentNullException(nameof(udpPacket.Buffer));
        }
        if(RadiusNasIdentifierExtractor.TryExtract(udpPacket.Buffer, out var nasIdentifier))
            clientConfiguration = _serviceConfiguration.GetClientConfiguration(nasIdentifier);
        clientConfiguration ??= _serviceConfiguration.GetClientConfiguration(udpPacket.RemoteEndPoint.Address);

        return clientConfiguration;
    }

    private bool IsRetransmission(RadiusPacket requestPacket)
    {
        var packetKey = CreateUniquePacketKey(requestPacket);
        if (_cache.HasValue(packetKey))
            return true;
        _cache.Set(packetKey);
        return false;
    }

    private static string CreateUniquePacketKey(RadiusPacket requestPacket)
    {
        var base64Authenticator = Convert.ToBase64String(requestPacket.Authenticator.Value);
        return $"{requestPacket.Code:d}:{requestPacket.Identifier}:{requestPacket.RemoteEndpoint}:{requestPacket.UserName}:{base64Authenticator}";
    }
}