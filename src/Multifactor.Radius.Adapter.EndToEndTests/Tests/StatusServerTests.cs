using MultiFactor.Radius.Adapter.Core.Radius;
using Multifactor.Radius.Adapter.EndToEndTests.Constants;
using Multifactor.Radius.Adapter.EndToEndTests.Fixtures.Models;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration.Models;

namespace Multifactor.Radius.Adapter.EndToEndTests.Tests;

[Collection("Radius e2e")]
public class StatusServerTests(RadiusFixtures radiusFixtures) : E2ETestBase(radiusFixtures)
{
    [Fact]
    public async Task GetServerStatus_ShouldSuccess()
    {
        var rootConfig = new RadiusAdapterConfiguration()
        {
            AppSettings = new AppSettingsSection()
            {
                AdapterServerEndpoint = "0.0.0.0:1812",
                MultifactorApiUrl = "https://api.multifactor.dev",
                LoggingLevel = "Debug",
                RadiusSharedSecret = RadiusAdapterConstants.DefaultSharedSecret,
                RadiusClientNasIdentifier = RadiusAdapterConstants.DefaultNasIdentifier,
                BypassSecondFactorWhenApiUnreachable = true,
                MultifactorNasIdentifier = "nas-identifier",
                MultifactorSharedSecret = "shared-secret",
                FirstFactorAuthenticationSource = "None"
            }
        };
        
        await StartHostAsync(new E2ERadiusConfiguration(rootConfig));

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