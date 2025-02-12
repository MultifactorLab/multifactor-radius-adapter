using MultiFactor.Radius.Adapter.Core.Radius;
using Multifactor.Radius.Adapter.EndToEndTests.Constants;

namespace Multifactor.Radius.Adapter.EndToEndTests.Tests;

[Collection("Radius e2e")]
public class StatusServerTests(RadiusFixtures radiusFixtures) : E2ETestBase(radiusFixtures)
{
    [Fact]
    public async Task GetServerStatus_ShouldSuccess()
    {
        await StartHostAsync(RadiusAdapterConfigs.RootConfig, [RadiusAdapterConfigs.StatusServerConfig]);

        var serverStatusPacket = CreateRadiusPacket(PacketCode.StatusServer);
        
        serverStatusPacket!.AddAttributes(new Dictionary<string, object>() { { "NAS-Identifier", RadiusAdapterConstants.DefaultNasIdentifier } });

        var response = SendPacketAsync(serverStatusPacket);

        Assert.NotNull(response);
        Assert.Equal(PacketCode.AccessAccept, response.Header.Code);

        var replyMessage = response.Attributes["Reply-Message"].FirstOrDefault()?.ToString();
        Assert.NotNull(replyMessage);
        Assert.StartsWith("Server up", replyMessage);
    }
}