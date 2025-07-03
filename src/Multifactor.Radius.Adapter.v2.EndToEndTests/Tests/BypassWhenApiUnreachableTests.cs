using Microsoft.Extensions.Hosting;
using Moq;
using Multifactor.Radius.Adapter.v2.Core.Auth;
using Multifactor.Radius.Adapter.v2.Core.MultifactorApi;
using Multifactor.Radius.Adapter.v2.Core.Radius.Packet;
using Multifactor.Radius.Adapter.v2.EndToEndTests.Constants;
using Multifactor.Radius.Adapter.v2.EndToEndTests.Fixtures;
using Multifactor.Radius.Adapter.v2.EndToEndTests.Fixtures.Models;
using Multifactor.Radius.Adapter.v2.Exceptions;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configuration.RadiusAdapter;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configuration.RadiusAdapter.Sections;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configuration.RadiusAdapter.Sections.LdapServer;
using Multifactor.Radius.Adapter.v2.Services.MultifactorApi;

namespace Multifactor.Radius.Adapter.v2.EndToEndTests.Tests;

[Collection("Radius e2e")]
public class BypassWhenApiUnreachableTests(RadiusFixtures radiusFixtures) : E2ETestBase(radiusFixtures)
{
    [Fact]
    public async Task BST001_ShouldAccept()
    {
        var secondFactorMock = new Mock<IMultifactorApiService>();

        secondFactorMock
            .Setup(x => x.CreateSecondFactorRequestAsync(It.IsAny<CreateSecondFactorRequest>()))
            .ReturnsAsync(new MultifactorResponse(AuthenticationStatus.Accept));

        var hostConfiguration = (HostApplicationBuilder builder) =>
        {
            builder.Services.ReplaceService(secondFactorMock.Object);
        };

        var sensitiveData = E2ETestsUtils.GetConfigSensitiveData("root.ad.env");
        var rootConfig = CreateRootConfig(sensitiveData);

        await StartHostAsync(rootConfig, configure: hostConfiguration);

        var accessRequest = CreateRadiusPacket(PacketCode.AccessRequest);
        accessRequest.AddAttributeValue("NAS-Identifier", RadiusAdapterConstants.DefaultNasIdentifier);
        accessRequest.AddAttributeValue("User-Name", RadiusAdapterConstants.BindUserName);
        accessRequest.AddAttributeValue("User-Password", RadiusAdapterConstants.BindUserPassword);

        var response = SendPacketAsync(accessRequest);

        Assert.NotNull(response);
        Assert.Single(secondFactorMock.Invocations);
        Assert.Equal(PacketCode.AccessAccept, response.Code);
    }

    [Fact]
    public async Task BST002_ShouldAccept()
    {
        var secondFactorMock = new Mock<IMultifactorApiService>();

        secondFactorMock
            .Setup(x => x.CreateSecondFactorRequestAsync(It.IsAny<CreateSecondFactorRequest>()))
            .ReturnsAsync(new MultifactorResponse(AuthenticationStatus.Accept));

        var hostConfiguration = (HostApplicationBuilder builder) =>
        {
            builder.Services.ReplaceService(secondFactorMock.Object);
        };

        var sensitiveData = E2ETestsUtils.GetConfigSensitiveData("root.ad.env");
        var rootConfig = CreateRootConfig(sensitiveData, true);
        
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

    [Fact]
    public async Task BST003_ShouldAccept()
    {
        var secondFactorMock = new Mock<IMultifactorApi>();

        secondFactorMock
            .Setup(x => x.CreateAccessRequest(It.IsAny<AccessRequest>(), It.IsAny<ApiCredential>()))
            .ThrowsAsync(new MultifactorApiUnreachableException());

        var hostConfiguration = (HostApplicationBuilder builder) =>
        {
            builder.Services.ReplaceService(secondFactorMock.Object);
        };

        var sensitiveData = E2ETestsUtils.GetConfigSensitiveData("root.ad.env");
        var rootConfig = CreateRootConfig(sensitiveData);
        
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

    [Fact]
    public async Task BST004_ShouldReject()
    {
        var secondFactorMock = new Mock<IMultifactorApi>();

        secondFactorMock
            .Setup(x => x.CreateAccessRequest(It.IsAny<AccessRequest>(), It.IsAny<ApiCredential>()))
            .ThrowsAsync(new MultifactorApiUnreachableException());

        var hostConfiguration = (HostApplicationBuilder builder) =>
        {
            builder.Services.ReplaceService(secondFactorMock.Object);
        };

        var sensitiveData = E2ETestsUtils.GetConfigSensitiveData("root.ad.env");
        var rootConfig = CreateRootConfig(sensitiveData, false);
        
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
        Assert.Equal(PacketCode.AccessReject, response.Code);
    }

    [Fact]
    public async Task BST005_ShouldAccept()
    {
        var secondFactorMock = new Mock<IMultifactorApi>();

        secondFactorMock
            .Setup(x => x.CreateAccessRequest(It.IsAny<AccessRequest>(), It.IsAny<ApiCredential>()))
            .ThrowsAsync(new MultifactorApiUnreachableException());

        var hostConfiguration = (HostApplicationBuilder builder) =>
        {
            builder.Services.ReplaceService(secondFactorMock.Object);
        };

        var sensitiveData = E2ETestsUtils.GetConfigSensitiveData("root.ad.env");
        var rootConfig = CreateRootConfig(sensitiveData, true);
        
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

    [Fact]
    public async Task BST006_ShouldAccept()
    {
        var secondFactorMock = new Mock<IMultifactorApiService>();

        secondFactorMock
            .Setup(x => x.CreateSecondFactorRequestAsync(It.IsAny<CreateSecondFactorRequest>()))
            .ReturnsAsync(new MultifactorResponse(AuthenticationStatus.Accept));

        var hostConfiguration = (HostApplicationBuilder builder) =>
        {
            builder.Services.ReplaceService(secondFactorMock.Object);
        };

        var sensitiveData = E2ETestsUtils.GetConfigSensitiveData("root.ad.env");
        var rootConfig = CreateRootConfig(sensitiveData, false);
        
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

    private RadiusAdapterConfiguration CreateRootConfig(ConfigSensitiveData[] sensitiveData, bool? bypassSecondFactorWhenApiUnreachable = null)
    {
        var configName = "root";
        return new RadiusAdapterConfiguration()
        {
            
            AppSettings = new AppSettingsSection()
            {
                AdapterServerEndpoint = "0.0.0.0:1812",
                MultifactorApiUrl = "https://api.multifactor.dev",
                LoggingLevel = "Debug",
                RadiusSharedSecret = RadiusAdapterConstants.DefaultSharedSecret,
                RadiusClientNasIdentifier = RadiusAdapterConstants.DefaultNasIdentifier,
                MultifactorNasIdentifier = "nas-identifier",
                MultifactorSharedSecret = "shared-secret",
                FirstFactorAuthenticationSource = "None",
                BypassSecondFactorWhenApiUnreachable = bypassSecondFactorWhenApiUnreachable ?? true
            },
            
            LdapServers = new LdapServersSection()
            {
                LdapServer = new LdapServerConfiguration()
                {
                    ConnectionString = sensitiveData.GetConfigValue(configName, nameof(LdapServerConfiguration.ConnectionString))!,
                    UserName = RadiusAdapterConstants.AdminUserName,
                    Password = RadiusAdapterConstants.AdminUserPassword,
                }
            }
        };
    }
}