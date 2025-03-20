using System.Text;
using Moq;
using MultiFactor.Radius.Adapter.Core.Framework;
using MultiFactor.Radius.Adapter.Core.Framework.Context;
using MultiFactor.Radius.Adapter.Core.Radius;
using Multifactor.Radius.Adapter.EndToEndTests.Constants;
using Multifactor.Radius.Adapter.EndToEndTests.Fixtures;
using Multifactor.Radius.Adapter.EndToEndTests.Fixtures.Models;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration.Models;
using MultiFactor.Radius.Adapter.Server;
using MultiFactor.Radius.Adapter.Services.MultiFactorApi;
using MultiFactor.Radius.Adapter.Services.MultiFactorApi.Models;

namespace Multifactor.Radius.Adapter.EndToEndTests.Tests;

[Collection("Radius e2e")]
public class PreSecondFactorTests(RadiusFixtures radiusFixtures) : E2ETestBase(radiusFixtures)
{
    [Theory]
    [InlineData("none-root-conf.env")]
    [InlineData("ad-root-conf.env")]
    [InlineData("radius-root-conf.env")]
    public async Task BST021_ShouldAccept(string configName)
    {
        var challenge1 = "challenge-1";
        var state = "BST021_ShouldAccept";

        var sensitiveData =
            E2ETestsUtils.GetConfigSensitiveData(configName);

        var mfAPiMock = new Mock<IMultifactorApiAdapter>();

        mfAPiMock
            .Setup(x => x.CreateSecondFactorRequestAsync(It.IsAny<RadiusContext>()))
            .ReturnsAsync(new SecondFactorResponse(AuthenticationCode.Awaiting, state));

        mfAPiMock
            .Setup(x => x.ChallengeAsync(It.IsAny<RadiusContext>(), challenge1, It.IsAny<ChallengeIdentifier>()))
            .ReturnsAsync(new ChallengeResponse(AuthenticationCode.Accept));

        var hostConfiguration = (RadiusHostApplicationBuilder builder) =>
        {
            builder.Services.ReplaceService(mfAPiMock.Object);
        };

        var rootConfig = CreateRadiusConfiguration(sensitiveData);

        await StartHostAsync(
            rootConfig,
            configure: hostConfiguration);

        var defaultRequestAttributes = new Dictionary<string, object>()
        {
            { "NAS-Identifier", RadiusAdapterConstants.DefaultNasIdentifier },
            { "User-Name", RadiusAdapterConstants.BindUserName }
        };

        // AccessRequest step 1
        var accessRequest = CreateRadiusPacket(PacketCode.AccessRequest, identifier: 1);
        accessRequest!.AddAttributes(defaultRequestAttributes);
        accessRequest!.AddAttribute("User-Password", RadiusAdapterConstants.BindUserPassword);

        var response = SendPacketAsync(accessRequest);

        Assert.NotNull(response);
        Assert.Single(mfAPiMock.Invocations);
        Assert.Equal(PacketCode.AccessChallenge, response.Header.Code);
        Assert.NotEmpty(response.Attributes["State"]);
        var responseState = Encoding.UTF8.GetString((byte[])response.Attributes["State"].First());
        Assert.Equal(responseState, state);

        // Challenge step 2
        accessRequest = CreateRadiusPacket(PacketCode.AccessRequest);
        accessRequest!.AddAttributes(defaultRequestAttributes);
        accessRequest!.AddAttribute("State", state);
        accessRequest!.AddAttribute("User-Password", challenge1);

        response = SendPacketAsync(accessRequest);

        Assert.NotNull(response);
        Assert.Equal(2, mfAPiMock.Invocations.Count);
        Assert.Equal(PacketCode.AccessAccept, response.Header.Code);
    }

    private RadiusAdapterConfiguration CreateRadiusConfiguration(ConfigSensitiveData[] sensitiveData)
    {
        var configName = "root";
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

                PreAuthenticationMethod = "push",
                InvalidCredentialDelay = "3",

                ActiveDirectoryDomain = sensitiveData.GetConfigValue(
                    configName,
                    nameof(AppSettingsSection.ActiveDirectoryDomain)),

                NpsServerEndpoint = sensitiveData.GetConfigValue(
                    configName,
                    nameof(AppSettingsSection.NpsServerEndpoint)),

                AdapterClientEndpoint = sensitiveData.GetConfigValue(
                    configName,
                    nameof(AppSettingsSection.AdapterClientEndpoint)),

                FirstFactorAuthenticationSource = sensitiveData.GetConfigValue(
                    configName,
                    nameof(AppSettingsSection.FirstFactorAuthenticationSource))
            }
        };

        return rootConfig;
    }
}