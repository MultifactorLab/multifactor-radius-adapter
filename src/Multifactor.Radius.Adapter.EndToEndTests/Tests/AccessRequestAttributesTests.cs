using Moq;
using MultiFactor.Radius.Adapter.Core.Framework;
using MultiFactor.Radius.Adapter.Core.Radius;
using Multifactor.Radius.Adapter.EndToEndTests.Constants;
using Multifactor.Radius.Adapter.EndToEndTests.Fixtures;
using Multifactor.Radius.Adapter.EndToEndTests.Fixtures.Models;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration.Models;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration.Models.RadiusReply;
using MultiFactor.Radius.Adapter.Infrastructure.Http;
using MultiFactor.Radius.Adapter.Services.MultiFactorApi;
using MultiFactor.Radius.Adapter.Services.MultiFactorApi.Dto;

namespace Multifactor.Radius.Adapter.EndToEndTests.Tests;

[Collection("Radius e2e")]
public class AccessRequestAttributesTests(RadiusFixtures radiusFixtures) : E2ETestBase(radiusFixtures)
{
    [Theory]
    [InlineData("none-root-access-request-attributes.env")]
    [InlineData("ad-root-access-request-attributes.env")]
    [InlineData("radius-root-access-request-attributes.env")]
    public async Task BST026_ShouldAcceptAndSendAttributes(string configName)
    {
        var sensitiveData =
            E2ETestsUtils.GetConfigSensitiveData(configName, "__");

        var mfAPiMock = new Mock<IMultifactorApiClient>();
        CreateRequestDto payload = null;
        mfAPiMock
            .Setup(x => x.CreateRequestAsync(It.IsAny<CreateRequestDto>(), It.IsAny<BasicAuthHeaderValue>()))
            .Callback((CreateRequestDto x, BasicAuthHeaderValue y) => payload = x)
            .ReturnsAsync(new AccessRequestDto() { Status = RequestStatus.Granted} );

        var hostConfiguration = (RadiusHostApplicationBuilder builder) =>
        {
            builder.Services.ReplaceService(mfAPiMock.Object);
        };

        var rootConfig = CreateRadiusConfiguration(sensitiveData);

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
        Assert.Single(mfAPiMock.Invocations);
        Assert.Equal(PacketCode.AccessAccept, response.Header.Code);
        Assert.NotNull(payload);
        Assert.NotEmpty(payload.Email);
        Assert.NotEmpty(payload.Name);
        Assert.NotEmpty(payload.Phone);
    }
    
    [Theory]
    [InlineData("none-root-access-request-attributes.env", "Partial:RemoteHost")]
    [InlineData("ad-root-access-request-attributes.env", "Partial:RemoteHost")]
    [InlineData("radius-root-access-request-attributes.env", "Partial:RemoteHost")]
    [InlineData("none-root-access-request-attributes.env", "Full")]
    [InlineData("ad-root-access-request-attributes.env", "Full")]
    [InlineData("radius-root-access-request-attributes.env", "Full")]
    public async Task BST027_ShouldAcceptAndNotSendAttributes(string configName, string privacyMode)
    {
        var sensitiveData =
            E2ETestsUtils.GetConfigSensitiveData(configName, "__");

        var mfAPiMock = new Mock<IMultifactorApiClient>();
        CreateRequestDto payload = null;
        mfAPiMock
            .Setup(x => x.CreateRequestAsync(It.IsAny<CreateRequestDto>(), It.IsAny<BasicAuthHeaderValue>()))
            .Callback((CreateRequestDto x, BasicAuthHeaderValue y) => payload = x)
            .ReturnsAsync(new AccessRequestDto() { Status = RequestStatus.Granted} );

        var hostConfiguration = (RadiusHostApplicationBuilder builder) =>
        {
            builder.Services.ReplaceService(mfAPiMock.Object);
        };

        var rootConfig = CreateRadiusConfiguration(sensitiveData, privacyMode: privacyMode);

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
        Assert.Single(mfAPiMock.Invocations);
        Assert.Equal(PacketCode.AccessAccept, response.Header.Code);
        Assert.NotNull(payload);
        Assert.Null(payload.Email);
        Assert.Null(payload.Name);
        Assert.Null(payload.Phone);
    }

    private RadiusAdapterConfiguration CreateRadiusConfiguration(ConfigSensitiveData[] sensitiveData, string privacyMode = null)

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

                NpsServerEndpoint = sensitiveData.GetConfigValue(
                    configName,
                    nameof(AppSettingsSection.NpsServerEndpoint)),

                AdapterClientEndpoint = sensitiveData.GetConfigValue(
                    configName,
                    nameof(AppSettingsSection.AdapterClientEndpoint)),

                FirstFactorAuthenticationSource = sensitiveData.GetConfigValue(
                    configName,
                    nameof(AppSettingsSection.FirstFactorAuthenticationSource)),

                ServiceAccountPassword = sensitiveData.GetConfigValue(
                    configName,
                    nameof(AppSettingsSection.ServiceAccountPassword)),

                ServiceAccountUser = sensitiveData.GetConfigValue(
                    configName,
                    nameof(AppSettingsSection.ServiceAccountUser)),

                PhoneAttribute = "mobile",
                PrivacyMode = privacyMode
            },

            RadiusReply = new RadiusReplySection()
            {
                Attributes = new RadiusReplyAttributesSection(singleElement: new RadiusReplyAttribute()
                    { Name = "Class", From = "memberOf" })
            }
        };

        return rootConfig;
    }
}