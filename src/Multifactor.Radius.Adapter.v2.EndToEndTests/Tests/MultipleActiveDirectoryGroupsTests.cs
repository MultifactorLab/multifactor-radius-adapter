using Microsoft.Extensions.Hosting;
using Moq;
using Multifactor.Radius.Adapter.v2.Core.Auth;
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
public class MultipleActiveDirectoryGroupsTests(RadiusFixtures radiusFixtures) : E2ETestBase(radiusFixtures)
{
    [Theory]
    [InlineData("ad-root-conf.env")]
    public async Task BST009_ShouldAccept(string configName)
    {
        var sensitiveData =
            E2ETestsUtils.GetConfigSensitiveData(configName);

        var secondFactorMock = new Mock<IMultifactorApiService>();

        secondFactorMock
            .Setup(x => x.CreateSecondFactorRequestAsync(It.IsAny<CreateSecondFactorRequest>()))
            .ReturnsAsync(new MultifactorResponse(AuthenticationStatus.Accept));

        var hostConfiguration = (HostApplicationBuilder builder) =>
        {
            builder.Services.ReplaceService(secondFactorMock.Object);
        };
        
        var rootConfig = CreateRadiusConfiguration(sensitiveData, $"cn=Not,dc=Existed,dc=Group;{ sensitiveData.GetConfigValue("root", "AccessGroups")! }");

        await StartHostAsync(
            rootConfig,
            configure: hostConfiguration);

        var accessRequest = CreateRadiusPacket(PacketCode.AccessRequest);
        accessRequest.AddAttributeValue("NAS-Identifier", RadiusAdapterConstants.DefaultNasIdentifier);
        accessRequest.AddAttributeValue("User-Name", RadiusAdapterConstants.BindUserName);
        accessRequest.AddAttributeValue("User-Password", RadiusAdapterConstants.BindUserPassword);

        var response = SendPacketAsync(accessRequest);

        Assert.NotNull(response);
        Assert.Single(secondFactorMock.Invocations);
        Assert.Equal(PacketCode.AccessAccept, response.Code);
    }
    
    [Theory]
    [InlineData("ad-root-conf.env")]
    public async Task BST009_NestedGroup_ShouldAccept(string configName)
    {
        var sensitiveData =
            E2ETestsUtils.GetConfigSensitiveData(configName);

        var secondFactorMock = new Mock<IMultifactorApiService>();

        secondFactorMock
            .Setup(x => x.CreateSecondFactorRequestAsync(It.IsAny<CreateSecondFactorRequest>()))
            .ReturnsAsync(new MultifactorResponse(AuthenticationStatus.Accept));

        var hostConfiguration = (HostApplicationBuilder builder) =>
        {
            builder.Services.ReplaceService(secondFactorMock.Object);
        };
        
        var rootConfig = CreateRadiusConfiguration(sensitiveData, $"cn=Not,dc=Existed,dc=Group;{ sensitiveData.GetConfigValue("root", "NestedAccessGroups")! }");

        await StartHostAsync(
            rootConfig,
            configure: hostConfiguration);

        var accessRequest = CreateRadiusPacket(PacketCode.AccessRequest);
        accessRequest.AddAttributeValue("NAS-Identifier", RadiusAdapterConstants.DefaultNasIdentifier);
        accessRequest.AddAttributeValue("User-Name", RadiusAdapterConstants.BindUserName);
        accessRequest.AddAttributeValue("User-Password", RadiusAdapterConstants.BindUserPassword);

        var response = SendPacketAsync(accessRequest);

        Assert.NotNull(response);
        Assert.Single(secondFactorMock.Invocations);
        Assert.Equal(PacketCode.AccessAccept, response.Code);
    }

    [Theory]
    [InlineData("ad-root-conf.env")]
    public async Task BST010_ShouldAccept(string configName)
    {
        var sensitiveData =
            E2ETestsUtils.GetConfigSensitiveData(configName);

        var secondFactorMock = new Mock<IMultifactorApiService>();

        secondFactorMock
            .Setup(x => x.CreateSecondFactorRequestAsync(It.IsAny<CreateSecondFactorRequest>()))
            .ReturnsAsync(new MultifactorResponse(AuthenticationStatus.Accept));

        var hostConfiguration = (HostApplicationBuilder builder) =>
        {
            builder.Services.ReplaceService(secondFactorMock.Object);
        };

        var rootConfig = CreateRadiusConfiguration(sensitiveData, "cn=Not,dc=Existed,dc=Group1;cn=Not,dc=Existed,dc=Group2");

        await StartHostAsync(
            rootConfig,
            configure: hostConfiguration);

        var accessRequest = CreateRadiusPacket(PacketCode.AccessRequest);
        accessRequest.AddAttributeValue("NAS-Identifier", RadiusAdapterConstants.DefaultNasIdentifier);
        accessRequest.AddAttributeValue("User-Name", RadiusAdapterConstants.BindUserName);
        accessRequest.AddAttributeValue("User-Password", RadiusAdapterConstants.BindUserPassword);

        var response = SendPacketAsync(accessRequest);

        Assert.NotNull(response);
        Assert.Empty(secondFactorMock.Invocations);
        Assert.Equal(PacketCode.AccessReject, response.Code);
    }

    private RadiusAdapterConfiguration CreateRadiusConfiguration(
        ConfigSensitiveData[] sensitiveData,
        string activeDirectoryGroups)
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
                FirstFactorAuthenticationSource = sensitiveData.GetConfigValue(
                    configName,
                    nameof(AppSettingsSection.FirstFactorAuthenticationSource))!,
            },
            
            LdapServers = new LdapServersSection()
            {
                LdapServer = new LdapServerConfiguration()
                {
                    ConnectionString = sensitiveData.GetConfigValue(configName, nameof(LdapServerConfiguration.ConnectionString))!,
                    UserName = RadiusAdapterConstants.AdminUserName,
                    Password = RadiusAdapterConstants.AdminUserPassword,
                    AccessGroups = activeDirectoryGroups,
                    LoadNestedGroups = true
                }
            }
        };

        return rootConfig;
    }
}