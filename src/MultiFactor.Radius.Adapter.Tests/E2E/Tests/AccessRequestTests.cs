using MultiFactor.Radius.Adapter.Core.Framework;
using MultiFactor.Radius.Adapter.Core.Radius;
using MultiFactor.Radius.Adapter.Tests.E2E.Constants;

namespace MultiFactor.Radius.Adapter.Tests.E2E.Tests;

[Collection("Radius e2e")]
public class AccessRequestTests : E2ETestBase
{
    public AccessRequestTests(RadiusFixtures radiusFixtures) : base(radiusFixtures)
    {
    }

    [Fact]
    public async Task SendAuthRequestWithoutCredentials_ShouldReject()
    {
        await StartHostAsync(RadiusAdapterConfigs.RootConfig, new[] { RadiusAdapterConfigs.AccessRequestConfig });

        var accessRequest = CreateRadiusPacket(PacketCode.AccessRequest);
        accessRequest.AddAttributes(new Dictionary<string, object>() { { "NAS-Identifier", RadiusAdapterConstants.DefaultNasIdentifier } });

        var response = SendPacketAsync(accessRequest);

        Assert.NotNull(response);
        Assert.Equal(PacketCode.AccessReject, response.Header.Code);
    }
}