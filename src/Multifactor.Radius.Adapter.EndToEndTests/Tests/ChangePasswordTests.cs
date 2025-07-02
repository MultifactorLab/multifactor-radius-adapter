using System.Text;
using Moq;
using MultiFactor.Radius.Adapter.Core.Framework;
using MultiFactor.Radius.Adapter.Core.Radius;
using Multifactor.Radius.Adapter.EndToEndTests.Constants;
using Multifactor.Radius.Adapter.EndToEndTests.Fixtures;
using Multifactor.Radius.Adapter.EndToEndTests.Fixtures.Models;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration.Models;
using MultiFactor.Radius.Adapter.Infrastructure.Http;
using MultiFactor.Radius.Adapter.Services.MultiFactorApi;
using MultiFactor.Radius.Adapter.Services.MultiFactorApi.Dto;

namespace Multifactor.Radius.Adapter.EndToEndTests.Tests;

[Collection("Radius e2e")]
public class ChangePasswordTests(RadiusFixtures radiusFixtures) : E2ETestBase(radiusFixtures)
{
    private static byte _packetId = 0;

    [Theory]
    [InlineData("ad-root-change-password-conf.env")]
    public async Task BST020_ShouldAccept(string configName)
    {
        var newPassword = "Qwerty456!"; 
        var currentPassword = RadiusAdapterConstants.ChangePasswordUserPassword;

        var sensitiveData =
            E2ETestsUtils.GetConfigSensitiveData(configName, "__");
        
        var mfAPiMock = new Mock<IMultifactorApiClient>();
        
        mfAPiMock
            .Setup(x => x.CreateRequestAsync(It.IsAny<CreateRequestDto>(), It.IsAny<BasicAuthHeaderValue>()))
            .ReturnsAsync(new AccessRequestDto() { Status = RequestStatus.Granted });
        
        var hostConfiguration = (RadiusHostApplicationBuilder builder) =>
        {
            builder.Services.ReplaceService(mfAPiMock.Object);
        };
        
        var rootConfig = CreateRadiusConfiguration(sensitiveData);
        
        await StartHostAsync(
            rootConfig,
            configure: hostConfiguration);
        
        // Password changing
        await ChangePassword(
            currentPassword: currentPassword,
            newPassword: newPassword,
            rootConfig);
        
        // Rollback
        await ChangePassword(
            currentPassword: newPassword,
            newPassword: currentPassword,
            rootConfig);
    }
    
    [Theory]
    [InlineData("ad-root-pre-auth-change-password-conf.env")]
    public async Task BST022_ShouldAccept(string configName)
    {
        var newPassword = "Qwerty456!";
        var currentPassword = RadiusAdapterConstants.ChangePasswordUserPassword;

        var sensitiveData =
            E2ETestsUtils.GetConfigSensitiveData(configName, "__");
        
        var mfAPiMock = new Mock<IMultifactorApiClient>();
        
        mfAPiMock
            .Setup(x => x.CreateRequestAsync(It.IsAny<CreateRequestDto>(), It.IsAny<BasicAuthHeaderValue>()))
            .ReturnsAsync(new AccessRequestDto() { Status = RequestStatus.Granted });
        
        var hostConfiguration = (RadiusHostApplicationBuilder builder) =>
        {
            builder.Services.ReplaceService(mfAPiMock.Object);
        };
        
        var rootConfig = CreateRadiusConfiguration(sensitiveData);
        
        await StartHostAsync(
            rootConfig,
            configure: hostConfiguration);
        
        // Password changing
        await ChangePassword(
            currentPassword: currentPassword,
            newPassword: newPassword,
            rootConfig);
        
        // Rollback
        await ChangePassword(
            currentPassword: newPassword,
            newPassword: currentPassword,
            rootConfig);
    }

    private async Task ChangePassword(
        string currentPassword,
        string newPassword,
        RadiusAdapterConfiguration rootConfig)
    {
        await SetAttributeForUserInCatalogAsync(
            RadiusAdapterConstants.ChangePasswordUserName,
            rootConfig,
            "pwdLastSet",
            0);
        
        var defaultRequestAttributes = new Dictionary<string, object>()
        {
            { "NAS-Identifier", RadiusAdapterConstants.DefaultNasIdentifier },
            { "User-Name", RadiusAdapterConstants.ChangePasswordUserName }
        };
        
        // AccessRequest step 1
        var accessRequest = CreateRadiusPacket(PacketCode.AccessRequest, identifier: _packetId++);
        accessRequest!.AddAttributes(defaultRequestAttributes);
        accessRequest!.AddAttribute("User-Password", currentPassword);
        
        var response = SendPacketAsync(accessRequest);
        
        Assert.NotNull(response);
        Assert.Equal(PacketCode.AccessChallenge, response.Header.Code);
        Assert.NotEmpty(response.Attributes["State"]);
        var responseState =  Encoding.UTF8.GetString((byte[])response.Attributes["State"].First());
        Assert.True(Guid.TryParse(responseState, out Guid state));
        var stateString = state.ToString();
        
        // New Password step 2
        accessRequest = CreateRadiusPacket(PacketCode.AccessRequest, identifier: _packetId++);
        accessRequest!.AddAttributes(defaultRequestAttributes);
        accessRequest!.AddAttribute("State", stateString);
        accessRequest!.AddAttribute("User-Password", newPassword);
        
        response = SendPacketAsync(accessRequest);
        
        Assert.NotNull(response);
        Assert.Equal(PacketCode.AccessChallenge, response.Header.Code);
        
        // Repeat password step 3
        accessRequest = CreateRadiusPacket(PacketCode.AccessRequest, identifier: _packetId++);
        accessRequest!.AddAttributes(defaultRequestAttributes);
        accessRequest!.AddAttribute("State",stateString);
        accessRequest!.AddAttribute("User-Password", newPassword);
        
        response = SendPacketAsync(accessRequest);
        
        Assert.NotNull(response);
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
                
                ServiceAccountPassword = sensitiveData.GetConfigValue(
                    configName,
                    nameof(AppSettingsSection.ServiceAccountPassword)),
                
                ServiceAccountUser = sensitiveData.GetConfigValue(
                    configName,
                    nameof(AppSettingsSection.ServiceAccountUser)),

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
                
                PreAuthenticationMethod = sensitiveData.GetConfigValue(configName, nameof(AppSettingsSection.PreAuthenticationMethod)),
                InvalidCredentialDelay = sensitiveData.GetConfigValue(configName, nameof(AppSettingsSection.InvalidCredentialDelay)),
            }
        };

        return rootConfig;
    }
}