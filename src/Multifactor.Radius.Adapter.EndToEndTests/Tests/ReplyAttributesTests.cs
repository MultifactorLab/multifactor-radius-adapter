using Moq;
using MultiFactor.Radius.Adapter.Core.Framework;
using MultiFactor.Radius.Adapter.Core.Framework.Context;
using MultiFactor.Radius.Adapter.Core.Radius;
using Multifactor.Radius.Adapter.EndToEndTests.Constants;
using Multifactor.Radius.Adapter.EndToEndTests.Fixtures;
using Multifactor.Radius.Adapter.EndToEndTests.Fixtures.Models;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration.Models;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration.Models.RadiusReply;
using MultiFactor.Radius.Adapter.Services.MultiFactorApi;
using MultiFactor.Radius.Adapter.Services.MultiFactorApi.Models;

namespace Multifactor.Radius.Adapter.EndToEndTests.Tests;

[Collection("Radius e2e")]
public class ReplyAttributesTests(RadiusFixtures radiusFixtures) : E2ETestBase(radiusFixtures)
{
    [Theory]
    [InlineData("none-root-reply-attributes.env")]
    [InlineData("ad-root-reply-attributes.env")]
    [InlineData("radius-root-reply-attributes.env")]
    public async Task BST025_ShouldAcceptAndReturnAttributes(string configName)
    {
        var sensitiveData =
            E2ETestsUtils.GetConfigSensitiveData(configName, "__");

        var mfAPiMock = new Mock<IMultifactorApiAdapter>();

        mfAPiMock
            .Setup(x => x.CreateSecondFactorRequestAsync(It.IsAny<RadiusContext>()))
            .ReturnsAsync(new SecondFactorResponse(AuthenticationCode.Accept));

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
        Assert.NotEmpty(response.Attributes);
        Assert.True(response.Attributes.ContainsKey("Class"));
        var classAttribute = response.Attributes["Class"];
        Assert.NotEmpty(classAttribute);
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
                    nameof(AppSettingsSection.ServiceAccountUser))
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