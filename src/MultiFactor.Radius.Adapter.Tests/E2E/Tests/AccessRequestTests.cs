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
        accessRequest.AddAttributes(new Dictionary<string, object>()
            { { "NAS-Identifier", RadiusAdapterConstants.DefaultNasIdentifier } });

        var response = SendPacketAsync(accessRequest);

        Assert.NotNull(response);
        Assert.Equal(PacketCode.AccessReject, response.Header.Code);
    }

    [Theory]
    [InlineData("adsettings.json", "E2ETestSensitiveSettings")]
    public async Task SendAuthRequestWithCredentials_ShouldAccept(string configName, string configSectionName)
    {
        var sensitiveData =
            E2ETestsUtils.GetSensitiveData<E2ETestSensitiveSettings>(configName, configSectionName);

        var envVariables = new Dictionary<string, string>()
        {
            {
                AdapterEnvironmentVariableNames.GetEnvironmentVariableName(
                    "access-request",
                    AdapterEnvironmentVariableNames.ActiveDirectoryDomain),
                sensitiveData.CatalogSettings.Hosts
            },
            {
                AdapterEnvironmentVariableNames.GetEnvironmentVariableName(
                    "access-request",
                    AdapterEnvironmentVariableNames.ServiceAccountUser),
                sensitiveData.TechUser.UserName
            },
            {
                AdapterEnvironmentVariableNames.GetEnvironmentVariableName(
                    "access-request",
                    AdapterEnvironmentVariableNames.ServiceAccountPassword),
                sensitiveData.TechUser.Password
            },
            {
                AdapterEnvironmentVariableNames.GetEnvironmentVariableName(
                    "access-request",
                    AdapterEnvironmentVariableNames.BypassSecondFactorWhenApiUnreachable),
                "true"
            }
        };
        
        await StartHostAsync(
            RadiusAdapterConfigs.RootConfig,
            new[] { RadiusAdapterConfigs.AccessRequestConfig },
            envVariables);

        var accessRequest = CreateRadiusPacket(PacketCode.AccessRequest);
        accessRequest.AddAttributes(new Dictionary<string, object>()
        {
            { "NAS-Identifier", RadiusAdapterConstants.DefaultNasIdentifier },
            { "User-Name", sensitiveData.User.UserName },
            { "User-Password", sensitiveData.User.Password }
        });

        var response = SendPacketAsync(accessRequest);

        Assert.NotNull(response);
        Assert.Equal(PacketCode.AccessAccept, response.Header.Code);
    }
}