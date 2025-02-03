using MultiFactor.Radius.Adapter.Core.Radius;
using MultiFactor.Radius.Adapter.Tests.E2E.Constants;

namespace MultiFactor.Radius.Adapter.Tests.E2E.Tests;

[Collection("Radius e2e")]
public class StatusServerTests : E2ETestBase
{
    public StatusServerTests(RadiusFixtures radiusFixtures) : base(radiusFixtures)
    {
    }

    [Fact]
    public async Task GetServerStatus_ShouldSuccess()
    {
        await StartHostAsync(RadiusAdapterConfigs.RootConfig, new[] { RadiusAdapterConfigs.StatusServerConfig });

        var serverStatusPacket = CreateRadiusPacket(PacketCode.StatusServer);
        
        serverStatusPacket.AddAttributes(new Dictionary<string, object>() { { "NAS-Identifier", RadiusAdapterConstants.DefaultNasIdentifier } });

        var response = SendPacketAsync(serverStatusPacket);

        Assert.NotNull(response);
        Assert.Equal(PacketCode.AccessAccept, response.Header.Code);

        var replyMessage = response.Attributes["Reply-Message"].FirstOrDefault()?.ToString();
        Assert.NotNull(replyMessage);
        Assert.StartsWith("Server up", replyMessage);
    }
}