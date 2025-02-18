using MultiFactor.Radius.Adapter.Core.Radius;
using Multifactor.Radius.Adapter.EndToEndTests.Constants;
using Multifactor.Radius.Adapter.EndToEndTests.Fixtures.Models;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration.Models;

namespace Multifactor.Radius.Adapter.EndToEndTests.Tests;

[Collection("Radius e2e")]
public class DefaultUsersBindTests(RadiusFixtures radiusFixtures) : E2ETestBase(radiusFixtures)
{
    [Theory]
    [InlineData("root.ad.env")]
    [InlineData("root.radius.env")]
    public async Task SendAuthRequestWithoutCredentials_RootConfig_ShouldReject(string configName)
    {
        var sensitiveData =
            E2ETestsUtils.GetConfigSensitiveData(configName);
        
        var clientConfigName = "root";
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
                    clientConfigName,
                    nameof(AppSettingsSection.ActiveDirectoryDomain)),
                        
                NpsServerEndpoint = sensitiveData.GetConfigValue(
                    clientConfigName,
                    nameof(AppSettingsSection.NpsServerEndpoint)),
                        
                AdapterClientEndpoint = sensitiveData.GetConfigValue(
                    clientConfigName,
                    nameof(AppSettingsSection.AdapterClientEndpoint)),
                        
                FirstFactorAuthenticationSource = sensitiveData.GetConfigValue(
                    clientConfigName,
                    nameof(AppSettingsSection.FirstFactorAuthenticationSource))
            }
        };
        
        await StartHostAsync(new E2ERadiusConfiguration(rootConfig));

        var accessRequest = CreateRadiusPacket(PacketCode.AccessRequest);
        accessRequest!.AddAttributes(new Dictionary<string, object>()
            { { "NAS-Identifier", RadiusAdapterConstants.DefaultNasIdentifier } });

        var response = SendPacketAsync(accessRequest);

        Assert.NotNull(response);
        Assert.Equal(PacketCode.AccessReject, response.Header.Code);
    }
    
    [Theory]
    [InlineData("root.ad.env")]
    [InlineData("root.radius.env")]
    public async Task SendAuthRequest_RootConfig_ShouldAccept(string configName)
    {
        var sensitiveData =
            E2ETestsUtils.GetConfigSensitiveData(configName);
        
        var clientConfigName = "root";
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
                    clientConfigName,
                    nameof(AppSettingsSection.ActiveDirectoryDomain)),
                        
                NpsServerEndpoint = sensitiveData.GetConfigValue(
                    clientConfigName,
                    nameof(AppSettingsSection.NpsServerEndpoint)),
                        
                AdapterClientEndpoint = sensitiveData.GetConfigValue(
                    clientConfigName,
                    nameof(AppSettingsSection.AdapterClientEndpoint)),
                        
                FirstFactorAuthenticationSource = sensitiveData.GetConfigValue(
                    clientConfigName,
                    nameof(AppSettingsSection.FirstFactorAuthenticationSource))
            }
        };
        
        await StartHostAsync(new E2ERadiusConfiguration(rootConfig));

        var accessRequest = CreateRadiusPacket(PacketCode.AccessRequest);
        
        accessRequest!.AddAttributes(new Dictionary<string, object>()
        {
            { "NAS-Identifier", RadiusAdapterConstants.DefaultNasIdentifier },
            { "User-Name", RadiusAdapterConstants.BindUserName },
            { "User-Password", RadiusAdapterConstants.BindUserPassword }
        });

        var response = SendPacketAsync(accessRequest);

        Assert.NotNull(response);
        Assert.Equal(PacketCode.AccessAccept, response.Header.Code);
    }

    [Theory]
    [InlineData("client.ad.env")]
    [InlineData("client.radius.env")]
    public async Task SendAuthRequestWithBindUser_ClientConfig_ShouldAccept(string configName)
    {
        var config = CreateRadiusConfiguration(configName);
        await StartHostAsync(config);

        var accessRequest = CreateRadiusPacket(PacketCode.AccessRequest);
        accessRequest!.AddAttributes(new Dictionary<string, object>()
        {
            { "NAS-Identifier", RadiusAdapterConstants.DefaultNasIdentifier },
            { "User-Name", RadiusAdapterConstants.BindUserName },
            { "User-Password", RadiusAdapterConstants.BindUserPassword }
        });

        var response = SendPacketAsync(accessRequest);

        Assert.NotNull(response);
        Assert.Equal(PacketCode.AccessAccept, response.Header.Code);
    }

    [Theory]
    [InlineData("client.ad.env")]
    [InlineData("client.radius.env")]
    public async Task SendAuthRequestWithAdminUser_ClientConfig_ShouldAccept(string configName)
    {
        var config = CreateRadiusConfiguration(configName);
        await StartHostAsync(config);

        var accessRequest = CreateRadiusPacket(PacketCode.AccessRequest);
        accessRequest!.AddAttributes(new Dictionary<string, object>()
        {
            { "NAS-Identifier", RadiusAdapterConstants.DefaultNasIdentifier },
            { "User-Name", RadiusAdapterConstants.AdminUserName },
            { "User-Password", RadiusAdapterConstants.AdminUserPassword }
        });

        var response = SendPacketAsync(accessRequest);

        Assert.NotNull(response);
        Assert.Equal(PacketCode.AccessAccept, response.Header.Code);
    }

    [Theory]
    [InlineData("client.ad.env")]
    [InlineData("client.radius.env")]
    public async Task SendAuthRequestWithPasswordUser_ClientConfig_ShouldAccept(string configName)
    {
        var config = CreateRadiusConfiguration(configName);
        await StartHostAsync(config);

        var accessRequest = CreateRadiusPacket(PacketCode.AccessRequest);
        accessRequest!.AddAttributes(new Dictionary<string, object>()
        {
            { "NAS-Identifier", RadiusAdapterConstants.DefaultNasIdentifier },
            { "User-Name", RadiusAdapterConstants.ChangePasswordUserName },
            { "User-Password", RadiusAdapterConstants.ChangePasswordUserPassword }
        });

        var response = SendPacketAsync(accessRequest);

        Assert.NotNull(response);
        Assert.Equal(PacketCode.AccessAccept, response.Header.Code);
    }

    private E2ERadiusConfiguration CreateRadiusConfiguration(string configName)
    {
        var sensitiveData =
            E2ETestsUtils.GetConfigSensitiveData(configName);

        var rootConfig = new RadiusAdapterConfiguration()
        {
            AppSettings = new AppSettingsSection()
            {
                AdapterServerEndpoint = "0.0.0.0:1812",
                MultifactorApiUrl = "https://api.multifactor.dev",
                LoggingLevel = "Debug"
            }
        };

        var clientConfigName = "client";
        var clientConfigs = new Dictionary<string, RadiusAdapterConfiguration>()
        {
            {
                clientConfigName, new RadiusAdapterConfiguration()
                {
                    AppSettings = new AppSettingsSection()
                    {
                        RadiusSharedSecret = RadiusAdapterConstants.DefaultSharedSecret,
                        RadiusClientNasIdentifier = RadiusAdapterConstants.DefaultNasIdentifier,
                        BypassSecondFactorWhenApiUnreachable = true,
                        MultifactorNasIdentifier = "nas-identifier",
                        MultifactorSharedSecret = "shared-secret",
                        
                        ActiveDirectoryDomain = sensitiveData.GetConfigValue(
                            clientConfigName,
                            nameof(AppSettingsSection.ActiveDirectoryDomain)),
                        
                        NpsServerEndpoint = sensitiveData.GetConfigValue(
                            clientConfigName,
                            nameof(AppSettingsSection.NpsServerEndpoint)),
                        
                        AdapterClientEndpoint = sensitiveData.GetConfigValue(
                            clientConfigName,
                            nameof(AppSettingsSection.AdapterClientEndpoint)),
                        
                        FirstFactorAuthenticationSource = sensitiveData.GetConfigValue(
                            clientConfigName,
                            nameof(AppSettingsSection.FirstFactorAuthenticationSource))
                    }
                }
            }
        };

        return new E2ERadiusConfiguration(rootConfig, clientConfigs);
    }
}