using System.Text;
using Microsoft.Extensions.Hosting;
using Moq;
using Multifactor.Core.Ldap.Name;
using Multifactor.Radius.Adapter.v2.Core.MultifactorApi;
using Multifactor.Radius.Adapter.v2.Core.Radius.Packet;
using Multifactor.Radius.Adapter.v2.EndToEndTests.Constants;
using Multifactor.Radius.Adapter.v2.EndToEndTests.Fixtures;
using Multifactor.Radius.Adapter.v2.EndToEndTests.Fixtures.Models;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configuration.RadiusAdapter;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configuration.RadiusAdapter.Sections;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configuration.RadiusAdapter.Sections.LdapServer;
using Multifactor.Radius.Adapter.v2.Services.MultifactorApi;

namespace Multifactor.Radius.Adapter.v2.EndToEndTests.Tests;

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

        var sensitiveData = E2ETestsUtils.GetConfigSensitiveData(configName, "__");
        
        var mfAPiMock = new Mock<IMultifactorApi>();
        
        mfAPiMock
            .Setup(x => x.CreateAccessRequest(It.IsAny<AccessRequest>(), It.IsAny<ApiCredential>()))
            .ReturnsAsync(new AccessRequestResponse() { Status = RequestStatus.Granted });
        
        var hostConfiguration = (HostApplicationBuilder builder) =>
        {
            builder.Services.ReplaceService(mfAPiMock.Object);
        };
        
        var rootConfig = CreateRadiusConfiguration(sensitiveData);
        
        await StartHostAsync(
            rootConfig,
            configure: hostConfiguration);
        
        var userDn = new DistinguishedName(sensitiveData.GetConfigValue("root", "UserDn")!);
        
        // Password changing
        ChangePassword(
            userDn,
            currentPassword: currentPassword,
            newPassword: newPassword,
            rootConfig);
        
        // Rollback
        ChangePassword(
            userDn,
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

        var sensitiveData = E2ETestsUtils.GetConfigSensitiveData(configName, "__");
        
        var mfAPiMock = new Mock<IMultifactorApi>();
        
        mfAPiMock
            .Setup(x => x.CreateAccessRequest(It.IsAny<AccessRequest>(), It.IsAny<ApiCredential>()))
            .ReturnsAsync(new AccessRequestResponse() { Status = RequestStatus.Granted });
        
        var hostConfiguration = (HostApplicationBuilder builder) =>
        {
            builder.Services.ReplaceService(mfAPiMock.Object);
        };
        
        var rootConfig = CreateRadiusConfiguration(sensitiveData);
        
        await StartHostAsync(
            rootConfig,
            configure: hostConfiguration);
        
        var userDn = new DistinguishedName(sensitiveData.GetConfigValue("root", "UserDn")!);
        
        // Password changing
        ChangePassword(
            userDn,
            currentPassword: currentPassword,
            newPassword: newPassword,
            rootConfig);
        
        // Rollback
        ChangePassword(
            userDn,
            currentPassword: newPassword,
            newPassword: currentPassword,
            rootConfig);
    }

    private void ChangePassword(
        DistinguishedName userDn,
        string currentPassword,
        string newPassword,
        RadiusAdapterConfiguration rootConfig)
    {
        SetAttributeForUserInCatalogAsync(
            userDn,
            rootConfig,
            "pwdLastSet",
            0);
        
        // AccessRequest step 1
        var accessRequest = CreateRadiusPacket(PacketCode.AccessRequest, identifier: _packetId++);
        accessRequest.AddAttributeValue("NAS-Identifier", RadiusAdapterConstants.DefaultNasIdentifier);
        accessRequest.AddAttributeValue("User-Name", RadiusAdapterConstants.ChangePasswordUserName);
        accessRequest.AddAttributeValue("User-Password", currentPassword);
        
        var response = SendPacketAsync(accessRequest);
        
        Assert.NotNull(response);
        Assert.Equal(PacketCode.AccessChallenge, response.Code);
        var attribute = response.Attributes["State"];
        Assert.NotNull(attribute.Values.FirstOrDefault());
        var attributeValue = attribute.Values.FirstOrDefault();
        var responseState =  Encoding.UTF8.GetString(attributeValue as byte[]);
        Assert.True(Guid.TryParse(responseState, out Guid state));
        var stateString = state.ToString();
        
        // New Password step 2
        accessRequest = CreateRadiusPacket(PacketCode.AccessRequest, identifier: _packetId++);
        accessRequest.AddAttributeValue("NAS-Identifier", RadiusAdapterConstants.DefaultNasIdentifier);
        accessRequest.AddAttributeValue("User-Name", RadiusAdapterConstants.ChangePasswordUserName);
        accessRequest.AddAttributeValue("State", stateString);
        accessRequest.AddAttributeValue("User-Password", newPassword);
        
        response = SendPacketAsync(accessRequest);
        
        Assert.NotNull(response);
        Assert.Equal(PacketCode.AccessChallenge, response.Code);
        
        // Repeat password step 3
        accessRequest = CreateRadiusPacket(PacketCode.AccessRequest, identifier: _packetId++);
        accessRequest.AddAttributeValue("NAS-Identifier", RadiusAdapterConstants.DefaultNasIdentifier);
        accessRequest.AddAttributeValue("User-Name", RadiusAdapterConstants.ChangePasswordUserName);
        accessRequest.AddAttributeValue("State",stateString);
        accessRequest.AddAttributeValue("User-Password", newPassword);
        
        response = SendPacketAsync(accessRequest);
        
        Assert.NotNull(response);
        Assert.Equal(PacketCode.AccessAccept, response.Code);
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
                
                NpsServerEndpoint = sensitiveData.GetConfigValue(
                    configName,
                    nameof(AppSettingsSection.NpsServerEndpoint))!,

                AdapterClientEndpoint = sensitiveData.GetConfigValue(
                    configName,
                    nameof(AppSettingsSection.AdapterClientEndpoint))!,

                FirstFactorAuthenticationSource = sensitiveData.GetConfigValue(
                    configName,
                    nameof(AppSettingsSection.FirstFactorAuthenticationSource))!,
                
                PreAuthenticationMethod = sensitiveData.GetConfigValue(configName, nameof(AppSettingsSection.PreAuthenticationMethod))!,
                InvalidCredentialDelay = sensitiveData.GetConfigValue(configName, nameof(AppSettingsSection.InvalidCredentialDelay))!,
            },
            LdapServers = new LdapServersSection()
            {
                LdapServer = new LdapServerConfiguration()
                {
                    ConnectionString = sensitiveData.GetConfigValue(configName, nameof(LdapServerConfiguration.ConnectionString))!,
                    UserName = sensitiveData.GetConfigValue(
                        configName,
                        nameof(LdapServerConfiguration.UserName))!,
                    Password = sensitiveData.GetConfigValue(
                        configName,
                        nameof(LdapServerConfiguration.Password))!,
                }
            }
        };

        return rootConfig;
    }
}