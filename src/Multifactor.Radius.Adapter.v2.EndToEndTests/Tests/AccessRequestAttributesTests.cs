using Microsoft.Extensions.Hosting;
using Moq;
using Multifactor.Radius.Adapter.v2.Core.MultifactorApi;
using Multifactor.Radius.Adapter.v2.Core.Radius.Packet;
using Multifactor.Radius.Adapter.v2.EndToEndTests.Constants;
using Multifactor.Radius.Adapter.v2.EndToEndTests.Fixtures;
using Multifactor.Radius.Adapter.v2.EndToEndTests.Fixtures.Models;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configuration.RadiusAdapter;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configuration.RadiusAdapter.Sections;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configuration.RadiusAdapter.Sections.LdapServer;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configuration.RadiusAdapter.Sections.RadiusReply;
using Multifactor.Radius.Adapter.v2.Services.MultifactorApi;

namespace Multifactor.Radius.Adapter.v2.EndToEndTests.Tests;

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

        var mfAPiMock = new Mock<IMultifactorApi>();
        AccessRequest? payload = null;
        mfAPiMock
            .Setup(x => x.CreateAccessRequest(It.IsAny<AccessRequest>(), It.IsAny<ApiCredential>()))
            .Callback((AccessRequest x, ApiCredential y) => payload = x)
            .ReturnsAsync(new AccessRequestResponse() { Status = RequestStatus.Granted} );

        var hostConfiguration = (HostApplicationBuilder builder) =>
        {
            builder.Services.ReplaceService(mfAPiMock.Object);
        };

        var rootConfig = CreateRadiusConfiguration(sensitiveData);

        await StartHostAsync(
            rootConfig,
            configure: hostConfiguration);

        var accessRequest = CreateRadiusPacket(PacketCode.AccessRequest);
        accessRequest.AddAttributeValue("NAS-Identifier", RadiusAdapterConstants.DefaultNasIdentifier);
        accessRequest.AddAttributeValue("User-Name", RadiusAdapterConstants.BindUserName);
        accessRequest.AddAttributeValue("User-Password", RadiusAdapterConstants.BindUserPassword);

        var response = SendPacketAsync(accessRequest);

        Assert.NotNull(response);
        Assert.Single(mfAPiMock.Invocations);
        Assert.Equal(PacketCode.AccessAccept, response.Code);
        Assert.NotNull(payload);
        Assert.False(string.IsNullOrWhiteSpace(payload.Email));
        Assert.False(string.IsNullOrWhiteSpace(payload.Name));
        Assert.False(string.IsNullOrWhiteSpace(payload.Phone));
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

        var mfAPiMock = new Mock<IMultifactorApi>();
        AccessRequest? payload = null;
        mfAPiMock
            .Setup(x => x.CreateAccessRequest(It.IsAny<AccessRequest>(), It.IsAny<ApiCredential>()))
            .Callback((AccessRequest x, ApiCredential y) => payload = x)
            .ReturnsAsync(new AccessRequestResponse() { Status = RequestStatus.Granted} );

        var hostConfiguration = (HostApplicationBuilder builder) =>
        {
            builder.Services.ReplaceService(mfAPiMock.Object);
        };

        var rootConfig = CreateRadiusConfiguration(sensitiveData, privacyMode: privacyMode);

        await StartHostAsync(
            rootConfig,
            configure: hostConfiguration);

        var accessRequest = CreateRadiusPacket(PacketCode.AccessRequest);
        accessRequest.AddAttributeValue("NAS-Identifier", RadiusAdapterConstants.DefaultNasIdentifier);
        accessRequest.AddAttributeValue("User-Name", RadiusAdapterConstants.BindUserName);
        accessRequest.AddAttributeValue("User-Password", RadiusAdapterConstants.BindUserPassword);

        var response = SendPacketAsync(accessRequest);

        Assert.NotNull(response);
        Assert.Single(mfAPiMock.Invocations);
        Assert.Equal(PacketCode.AccessAccept, response.Code);
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

                NpsServerEndpoint = sensitiveData.GetConfigValue(
                    configName,
                    nameof(AppSettingsSection.NpsServerEndpoint))!,

                AdapterClientEndpoint = sensitiveData.GetConfigValue(
                    configName,
                    nameof(AppSettingsSection.AdapterClientEndpoint))!,

                FirstFactorAuthenticationSource = sensitiveData.GetConfigValue(
                    configName,
                    nameof(AppSettingsSection.FirstFactorAuthenticationSource))!,
                
                PrivacyMode = privacyMode
            },
            
            LdapServers = new LdapServersSection()
            {
                LdapServer = new LdapServerConfiguration()
                {
                    ConnectionString = sensitiveData.GetConfigValue(configName, nameof(LdapServerConfiguration.ConnectionString))!,
                    UserName = RadiusAdapterConstants.AdminUserName,
                    Password = RadiusAdapterConstants.AdminUserPassword,
                    PhoneAttributes = "mobile"
                }
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