using Moq;
using MultiFactor.Radius.Adapter.Core.Framework;
using MultiFactor.Radius.Adapter.Core.Framework.Context;
using MultiFactor.Radius.Adapter.Core.Radius;
using Multifactor.Radius.Adapter.EndToEndTests.Constants;
using Multifactor.Radius.Adapter.EndToEndTests.Fixtures;
using Multifactor.Radius.Adapter.EndToEndTests.Fixtures.Models;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration.Models;
using MultiFactor.Radius.Adapter.Services.MultiFactorApi;
using MultiFactor.Radius.Adapter.Services.MultiFactorApi.Models;

namespace Multifactor.Radius.Adapter.EndToEndTests.Tests;

[Collection("Radius e2e")]
public class SingleActiveDirectory2FaBypassGroupTests(RadiusFixtures radiusFixtures) : E2ETestBase(radiusFixtures)
{
    [Theory]
    [InlineData("ad-root-conf.env")]
    public async Task BST014_ShouldAccept(string configName)
    {
        var sensitiveData =
            E2ETestsUtils.GetConfigSensitiveData(configName);

        var secondFactorMock = new Mock<IMultifactorApiAdapter>();

        secondFactorMock
            .Setup(x => x.CreateSecondFactorRequestAsync(It.IsAny<RadiusContext>()))
            .ReturnsAsync(new SecondFactorResponse(AuthenticationCode.Accept));

        var hostConfiguration = (RadiusHostApplicationBuilder builder) =>
        {
            builder.Services.ReplaceService(secondFactorMock.Object);
        };

        var rootConfig = CreateRadiusConfiguration(sensitiveData, "E2E");

        await StartHostAsync(
            rootConfig,
            configure: hostConfiguration);

        var accessRequest = CreateRadiusPacket(PacketCode.AccessRequest);
        accessRequest!.AddAttributes(new Dictionary<string, object>()
        {
            { "NAS-Identifier", RadiusAdapterConstants.DefaultNasIdentifier },
            { "User-Name", RadiusAdapterConstants.BindUserName },
            { "User-Password", RadiusAdapterConstants.BindUserPassword }
        });

        var response = SendPacketAsync(accessRequest);

        Assert.NotNull(response);
        Assert.Empty(secondFactorMock.Invocations);
        Assert.Equal(PacketCode.AccessAccept, response.Header.Code);
    }

    [Theory]
    [InlineData("ad-root-conf.env")]
    public async Task BST015_ShouldAccept(string configName)
    {
        var sensitiveData =
            E2ETestsUtils.GetConfigSensitiveData(configName);

        var secondFactorMock = new Mock<IMultifactorApiAdapter>();

        secondFactorMock
            .Setup(x => x.CreateSecondFactorRequestAsync(It.IsAny<RadiusContext>()))
            .ReturnsAsync(new SecondFactorResponse(AuthenticationCode.Accept));

        var hostConfiguration = (RadiusHostApplicationBuilder builder) =>
        {
            builder.Services.ReplaceService(secondFactorMock.Object);
        };

        var rootConfig = CreateRadiusConfiguration(sensitiveData, "Not-Existed-Group");

        await StartHostAsync(
            rootConfig,
            configure: hostConfiguration);

        var accessRequest = CreateRadiusPacket(PacketCode.AccessRequest);
        accessRequest!.AddAttributes(new Dictionary<string, object>()
        {
            { "NAS-Identifier", RadiusAdapterConstants.DefaultNasIdentifier },
            { "User-Name", RadiusAdapterConstants.BindUserName },
            { "User-Password", RadiusAdapterConstants.BindUserPassword }
        });

        var response = SendPacketAsync(accessRequest);

        Assert.NotNull(response);
        Assert.Single(secondFactorMock.Invocations);
        Assert.Equal(PacketCode.AccessAccept, response.Header.Code);
    }

    private RadiusAdapterConfiguration CreateRadiusConfiguration(
        ConfigSensitiveData[] sensitiveData,
        string activeDirectory2FaBypassGroup)
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

                ActiveDirectoryDomain = sensitiveData.GetConfigValue(
                    configName,
                    nameof(AppSettingsSection.ActiveDirectoryDomain)),

                FirstFactorAuthenticationSource = sensitiveData.GetConfigValue(
                    configName,
                    nameof(AppSettingsSection.FirstFactorAuthenticationSource)),

                ActiveDirectory2faBypassGroup = activeDirectory2FaBypassGroup
            }
        };

        return rootConfig;
    }
}