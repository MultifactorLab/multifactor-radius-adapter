using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Logging;
using Multifactor.Core.Ldap.LangFeatures;
using Multifactor.Radius.Adapter.v2.Core.Configuration.Client;
using Multifactor.Radius.Adapter.v2.Core.Configuration.Service;
using Multifactor.Radius.Adapter.v2.Core.Pipeline;
using Multifactor.Radius.Adapter.v2.Core.Pipeline.Settings;
using Multifactor.Radius.Adapter.v2.Core.Radius;
using Multifactor.Radius.Adapter.v2.Core.Radius.Packet;
using Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline;
using Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline.Context;
using Multifactor.Radius.Adapter.v2.Server.Udp;
using Multifactor.Radius.Adapter.v2.Services.Radius;

namespace Multifactor.Radius.Adapter.v2.Server;

public class UpdPacketHandler : IUdpPacketHandler
{
    private readonly ILogger<IUdpPacketHandler> _logger;
    private readonly IServiceConfiguration _serviceConfiguration;
    private readonly IRadiusPacketService _radiusPacketService;
    private readonly IPipelineProvider _pipelineProvider;
    private readonly IResponseSender _responseSender;

    public UpdPacketHandler(IServiceConfiguration serviceConfiguration, IRadiusPacketService packetService,
        IPipelineProvider pipelineProvider, IResponseSender responseSender, ILogger<IUdpPacketHandler> logger)
    {
        Throw.IfNull(serviceConfiguration, nameof(serviceConfiguration));
        Throw.IfNull(packetService, nameof(packetService));
        Throw.IfNull(pipelineProvider, nameof(pipelineProvider));
        Throw.IfNull(logger, nameof(logger));

        _serviceConfiguration = serviceConfiguration;
        _radiusPacketService = packetService;
        _pipelineProvider = pipelineProvider;
        _logger = logger;
        _responseSender = responseSender;
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

        var pipeline = _pipelineProvider.GetRadiusPipeline(clientConfiguration.Name);
        if (pipeline is null)
        {
            throw new Exception($"No pipeline found for client {clientConfiguration.Name}, check adapter configuration and restart the adapter.");
        }

        var requestPacket = _radiusPacketService.Parse(packetBytes, new SharedSecret(clientConfiguration.RadiusSharedSecret));

        await StartPipeline(clientConfiguration, requestPacket, remoteEndpoint, proxyEndpoint, pipeline);
    }

    private async Task StartPipeline(IClientConfiguration clientConfiguration, IRadiusPacket requestPacket, IPEndPoint remoteEndpoint, IPEndPoint? proxyEndpoint, IRadiusPipeline pipeline)
    {
        var executionSetting = new PipelineExecutionSettings(clientConfiguration);
        var context = new RadiusPipelineExecutionContext(executionSetting, requestPacket)
        {
            ProxyEndpoint = proxyEndpoint,
            RemoteEndpoint = remoteEndpoint
        };
        _logger.LogDebug("Start executing pipeline for '{name}'", clientConfiguration.Name);
        await pipeline.ExecuteAsync(context);
        await _responseSender.SendResponse(context);
    }

    private bool IsProxyProtocol(byte[] request, out IPEndPoint sourceEndpoint, out byte[] requestWithoutProxyHeader)
    {
        //https://www.haproxy.org/download/1.8/doc/proxy-protocol.txt

        sourceEndpoint = null;
        requestWithoutProxyHeader = null;

        if (request.Length < 6)
        {
            return false;
        }

        var proxySig = Encoding.ASCII.GetString(request.Take(5).ToArray());

        if (proxySig != "PROXY")
        {
            return false;
        }

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
}