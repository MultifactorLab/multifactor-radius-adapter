using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Multifactor.Radius.Adapter.v2.Core;
using Multifactor.Radius.Adapter.v2.Core.Configuration.Client;
using Multifactor.Radius.Adapter.v2.Core.Configuration.Service;
using Multifactor.Radius.Adapter.v2.Core.Pipeline;
using Multifactor.Radius.Adapter.v2.Core.Pipeline.Settings;
using Multifactor.Radius.Adapter.v2.Core.Radius;
using Multifactor.Radius.Adapter.v2.Core.Radius.Packet;
using Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline;
using Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline.Context;
using Multifactor.Radius.Adapter.v2.Server.Udp;
using Multifactor.Radius.Adapter.v2.Services.AdapterResponseSender;
using Multifactor.Radius.Adapter.v2.Services.Radius;

namespace Multifactor.Radius.Adapter.v2.Server;

public class UdpPacketHandler : IUdpPacketHandler
{
    private readonly ILogger<IUdpPacketHandler> _logger;
    private readonly IServiceConfiguration _serviceConfiguration;
    private readonly IRadiusPacketService _radiusPacketService;
    private readonly IPipelineProvider _pipelineProvider;
    private readonly IResponseSender _responseSender;
    private readonly IMemoryCache _memoryCache;

    public UdpPacketHandler(
        IServiceConfiguration serviceConfiguration,
        IRadiusPacketService packetService,
        IPipelineProvider pipelineProvider,
        IResponseSender responseSender,
        IMemoryCache memoryCache,
        ILogger<IUdpPacketHandler> logger)
    {
        _serviceConfiguration = serviceConfiguration;
        _radiusPacketService = packetService;
        _pipelineProvider = pipelineProvider;
        _logger = logger;
        _responseSender = responseSender;
        _memoryCache = memoryCache;
    }
    
    public async Task HandleUdpPacket(UdpReceiveResult udpPacket)
    {
        IPEndPoint? proxyEndpoint = null;
        var remoteEndpoint = udpPacket.RemoteEndPoint;
        var packetBytes = udpPacket.Buffer;

        if (IsProxyProtocol(packetBytes, out var sourceEndpoint, out var requestWithoutProxyHeader))
        {
            packetBytes = requestWithoutProxyHeader;
            proxyEndpoint = remoteEndpoint;
            remoteEndpoint = sourceEndpoint;
        }

        var clientConfiguration = GetClientConfig(udpPacket);
        if (clientConfiguration == null)
        {
            _logger.LogWarning("Received packet from unknown client {host:l}:{port}, ignoring", remoteEndpoint.Address, remoteEndpoint.Port);
            return;
        }

        var requestPacket = _radiusPacketService.Parse(packetBytes, new SharedSecret(clientConfiguration.RadiusSharedSecret));
        if (IsRetransmission(requestPacket, remoteEndpoint))
        {
            _logger.LogDebug("Duplicated request from {host:l}:{port} id={id} client '{client:l}', ignoring", remoteEndpoint.Address, remoteEndpoint.Port, requestPacket.Identifier, clientConfiguration.Name);
            return;
        }

        var pipeline = _pipelineProvider.GetRadiusPipeline(clientConfiguration.Name);
        if (pipeline is null)
            throw new Exception($"No pipeline found for client {clientConfiguration.Name}, check adapter configuration and restart the adapter.");
        
        await StartPipeline(clientConfiguration, requestPacket, remoteEndpoint, proxyEndpoint, pipeline);
    }

    private async Task StartPipeline(IClientConfiguration clientConfiguration, IRadiusPacket requestPacket, IPEndPoint remoteEndpoint, IPEndPoint? proxyEndpoint, IRadiusPipeline pipeline)
    {
        foreach (var serverConfig in clientConfiguration.LdapServers)
        {
            var executionSetting = new PipelineExecutionSettings(clientConfiguration, serverConfig);
            var context = new RadiusPipelineExecutionContext(executionSetting, requestPacket)
            {
                ProxyEndpoint = proxyEndpoint,
                RemoteEndpoint = remoteEndpoint,
                Passphrase = UserPassphrase.Parse(requestPacket.TryGetUserPassword(), clientConfiguration.PreAuthnMode)
            };
            
            _logger.LogDebug("Start executing pipeline for '{name}'", clientConfiguration.Name);
            
            try
            {
                await pipeline.ExecuteAsync(context);
                var responseRequest = GetResponseRequest(context);
                await _responseSender.SendResponse(responseRequest);
            }
            catch (Exception e)
            {
                _logger.LogWarning(exception: e, "Failed to execute pipeline for {name}", serverConfig.ConnectionString);
            }
        }
    }

    private bool IsProxyProtocol(byte[] request, out IPEndPoint sourceEndpoint, out byte[] requestWithoutProxyHeader)
    {
        //https://www.haproxy.org/download/1.8/doc/proxy-protocol.txt

        sourceEndpoint = null;
        requestWithoutProxyHeader = null;

        if (request.Length < 6)
            return false;

        var proxySig = Encoding.ASCII.GetString(request.Take(5).ToArray());

        if (proxySig != "PROXY")
            return false;

        var lf = Array.IndexOf(request, (byte)'\n');
        var headerBytes = request.Take(lf + 1).ToArray();
        var header = Encoding.ASCII.GetString(headerBytes);

        var parts = header.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        var sourceIp = parts[2];
        var sourcePort = int.Parse(parts[4]);

        sourceEndpoint = new IPEndPoint(IPAddress.Parse(sourceIp), sourcePort);
        requestWithoutProxyHeader = request.Skip(lf + 1).ToArray();

        return true;
    }

    private IClientConfiguration? GetClientConfig(UdpReceiveResult udpPacket)
    {
        IClientConfiguration? clientConfiguration = null;
        if (_radiusPacketService.TryGetNasIdentifier(udpPacket.Buffer, out var nasIdentifier))
            clientConfiguration = _serviceConfiguration.GetClient(nasIdentifier);
        else
            clientConfiguration ??= _serviceConfiguration.GetClient(udpPacket.RemoteEndPoint.Address);

        return clientConfiguration;
    }

    private SendAdapterResponseRequest GetResponseRequest(IRadiusPipelineExecutionContext context) => new(context);

    private bool IsRetransmission(IRadiusPacket requestPacket, IPEndPoint remoteEndpoint)
    {
        var packetKey = CreateUniquePacketKey(requestPacket, remoteEndpoint);
        if (_memoryCache.TryGetValue(packetKey, out _))
            return true;

        _memoryCache.Set(packetKey, 1, DateTimeOffset.UtcNow.AddMinutes(1));

        return false;
    }

    private string CreateUniquePacketKey(IRadiusPacket requestPacket, IPEndPoint remoteEndpoint)
    {
        var base64Authenticator = requestPacket.Authenticator.Value.Base64();
        return $"{requestPacket.Code:d}:{requestPacket.Identifier}:{remoteEndpoint}:{requestPacket.UserName}:{base64Authenticator}";
    }
}