using Microsoft.Extensions.Hosting;
using MultiFactor.Radius.Adapter.Core.Framework;
using MultiFactor.Radius.Adapter.Core.Radius;
using MultiFactor.Radius.Adapter.Tests.E2E.Udp;
using MultiFactor.Radius.Adapter.Tests.Fixtures.Radius;

namespace MultiFactor.Radius.Adapter.Tests.E2E;

public abstract class E2ETestBase : IDisposable
{
    private IHost? _host = null;
    private readonly RadiusHostApplicationBuilder _radiusHostApplicationBuilder;
    private readonly RadiusPacketParser _packetParser;
    private readonly SharedSecret _secret;
    private readonly UdpSocket _udpSocket;
    
    protected E2ETestBase(RadiusFixtures radiusFixtures)
    {
        _radiusHostApplicationBuilder = RadiusHost.CreateApplicationBuilder(new[] { "--environment", "Test" });
        _packetParser = radiusFixtures.Parser;
        _secret = radiusFixtures.SharedSecret;
        _udpSocket = radiusFixtures.UdpSocket;
    }
    
    private protected async Task StartHostAsync(Action<RadiusHostApplicationBuilder>? configure = null)
    {
        configure?.Invoke(_radiusHostApplicationBuilder);
        _host = _radiusHostApplicationBuilder.Build();
        
        await _host.StartAsync();
    }

    protected IRadiusPacket SendPacketAsync(IRadiusPacket radiusPacket)
    {
        if (radiusPacket is null)
        {
            throw new ArgumentNullException(nameof(radiusPacket));
        }

        var packetBytes = _packetParser.GetBytes(radiusPacket);
        _udpSocket.Send(packetBytes);
        
        var data = _udpSocket.Receive();
        var parsed = _packetParser.Parse(data.GetBytes(), _secret);
        
        return parsed;
    }

    protected IRadiusPacket CreateRadiusPacket(PacketCode packetCode, Dictionary<string,string> additionalAttributes = null)
    {
        IRadiusPacket packet = null;
        switch (packetCode)
        {
            case PacketCode.AccessRequest:
                packet = RadiusPacketFactory.AccessRequest();
                break;
            case PacketCode.StatusServer:
                packet = RadiusPacketFactory.StatusServer();
                break;
            case PacketCode.AccessChallenge:
                packet = RadiusPacketFactory.AccessChallenge();
                break;
            case PacketCode.AccessReject:
                packet = RadiusPacketFactory.AccessReject();
                break;
            default:
                throw new NotImplementedException();
        }

        if (additionalAttributes?.Count > 0)
        {
            foreach (var attribute in additionalAttributes)
            {
                packet.AddAttribute(attribute.Key, attribute.Value);
            }
        }
        
        return packet;
    }

    public void Dispose()
    {
        _host?.StopAsync();
        _host?.Dispose();
    }
}