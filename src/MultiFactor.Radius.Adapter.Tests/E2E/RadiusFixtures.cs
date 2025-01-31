using MultiFactor.Radius.Adapter.Core.Radius;
using MultiFactor.Radius.Adapter.Tests.E2E.Constants;
using MultiFactor.Radius.Adapter.Tests.E2E.Udp;

namespace MultiFactor.Radius.Adapter.Tests.E2E;

public class RadiusFixtures : IDisposable
{
    public RadiusPacketParser Parser { get; }

    public UdpSocket UdpSocket { get; }
    
    public SharedSecret SharedSecret { get; }

    public RadiusFixtures()
    {
        Parser = E2ETestsUtils.GetRadiusPacketParser();
        
        UdpSocket = E2ETestsUtils.GetUdpSocket(
            RadiusAdapterConstants.LocalHost,
            RadiusAdapterConstants.DefaultRadiusAdapterPort);
        
        SharedSecret = new SharedSecret(RadiusAdapterConstants.DefaultSharedSecret);
    }

    public void Dispose()
    {
        UdpSocket.Dispose();
    }
}

[CollectionDefinition("Radius e2e")]
public class RadiusFixturesCollection : ICollectionFixture<RadiusFixtures>
{
}