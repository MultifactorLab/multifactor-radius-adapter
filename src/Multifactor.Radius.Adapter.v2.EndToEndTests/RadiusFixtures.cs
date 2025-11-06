using Multifactor.Radius.Adapter.v2.Core.Radius;
using Multifactor.Radius.Adapter.v2.EndToEndTests.Constants;
using Multifactor.Radius.Adapter.v2.EndToEndTests.Udp;
using Multifactor.Radius.Adapter.v2.Services.Radius;

namespace Multifactor.Radius.Adapter.v2.EndToEndTests;

public class RadiusFixtures : IDisposable
{
    public IRadiusPacketService Parser { get; } = E2ETestsUtils.GetRadiusPacketParser();

    public UdpSocket UdpSocket { get; } = E2ETestsUtils.GetUdpSocket(
        RadiusAdapterConstants.LocalHost,
        RadiusAdapterConstants.DefaultRadiusAdapterPort);

    public SharedSecret SharedSecret { get; } = new(RadiusAdapterConstants.DefaultSharedSecret);

    public void Dispose()
    {
        UdpSocket.Dispose();
    }
}

[CollectionDefinition("Radius e2e")]
public class RadiusFixturesCollection : ICollectionFixture<RadiusFixtures>
{
}