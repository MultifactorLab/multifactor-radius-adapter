using MultiFactor.Radius.Adapter.Core.Radius;
using Multifactor.Radius.Adapter.EndToEndTests.Constants;
using Multifactor.Radius.Adapter.EndToEndTests.Udp;

namespace Multifactor.Radius.Adapter.EndToEndTests;

public class RadiusFixtures : IDisposable
{
    public RadiusPacketParser Parser { get; } = E2ETestsUtils.GetRadiusPacketParser();

    public UdpSocket UdpSocket { get; } = E2ETestsUtils.GetUdpSocket(
        RadiusAdapterConstants.LocalHost,
        RadiusAdapterConstants.DefaultRadiusAdapterPort);

    public SharedSecret? SharedSecret { get; } = new(RadiusAdapterConstants.DefaultSharedSecret);

    public void Dispose()
    {
        UdpSocket.Dispose();
    }
}

[CollectionDefinition("Radius e2e")]
public class RadiusFixturesCollection : ICollectionFixture<RadiusFixtures>
{
}