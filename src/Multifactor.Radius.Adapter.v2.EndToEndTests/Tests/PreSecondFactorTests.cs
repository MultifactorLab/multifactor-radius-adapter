using System.Text;
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
public class PreSecondFactorTests(RadiusFixtures radiusFixtures) : E2ETestBase(radiusFixtures)
{
    [Theory]
    [InlineData("none-root-conf.env")]
    [InlineData("ad-root-conf.env")]
    [InlineData("radius-root-conf.env")]
    public async Task BST021_ShouldAccept(string configName)
    {
        var state = "BST021_ShouldAccept";
        
        var sensitiveData =
            E2ETestsUtils.GetConfigSensitiveData(configName);
        
        var mfAPiMock = new Mock<IMultifactorApiService>();
        
        mfAPiMock
            .Setup(x => x.CreateSecondFactorRequestAsync(It.IsAny<CreateSecondFactorRequest>()))
            .ReturnsAsync(new MultifactorResponse(AuthenticationStatus.Accept, state));
        
        var hostConfiguration = (HostApplicationBuilder builder) =>
        {
            builder.Services.ReplaceService(mfAPiMock.Object);
        };

        var ldapServersSection = new LdapServersSection()
        {
            LdapServer = new LdapServerConfiguration()
            {
                ConnectionString =
                    sensitiveData.GetConfigValue("root", nameof(LdapServerConfiguration.ConnectionString))!,
                UserName = RadiusAdapterConstants.AdminUserName,
                Password = RadiusAdapterConstants.AdminUserPassword
            }
        };
        
        var rootConfig = CreateRadiusConfiguration(sensitiveData, ldapServersSection);
        
        await StartHostAsync(
            rootConfig,
            configure: hostConfiguration);
        
        // AccessRequest step 1
        var accessRequest = CreateRadiusPacket(PacketCode.AccessRequest);
        accessRequest.AddAttributeValue("NAS-Identifier", RadiusAdapterConstants.DefaultNasIdentifier);
        accessRequest.AddAttributeValue("User-Name", RadiusAdapterConstants.BindUserName);
        accessRequest.AddAttributeValue("User-Password", RadiusAdapterConstants.BindUserPassword);

        var response = SendPacketAsync(accessRequest);

        Assert.NotNull(response);
        Assert.Single(mfAPiMock.Invocations);
        Assert.Equal(PacketCode.AccessAccept, response.Code);
        Assert.False(response.Attributes.ContainsKey("State"));
    }
    
    [Theory]
    [InlineData("none-root-conf.env")]
    [InlineData("ad-root-conf.env")]
    [InlineData("radius-root-conf.env")]
    public async Task BST021_DomainUser_ShouldAccept(string configName)
    {
        var state = "BST021_ShouldAccept";
        
        var sensitiveData =
            E2ETestsUtils.GetConfigSensitiveData(configName);
        
        var mfAPiMock = new Mock<IMultifactorApiService>();
        
        mfAPiMock
            .Setup(x => x.CreateSecondFactorRequestAsync(It.IsAny<CreateSecondFactorRequest>()))
            .ReturnsAsync(new MultifactorResponse(AuthenticationStatus.Accept, state));
        
        var hostConfiguration = (HostApplicationBuilder builder) =>
        {
            builder.Services.ReplaceService(mfAPiMock.Object);
        };

        var ldapServersSection = new LdapServersSection()
        {
            LdapServer = new LdapServerConfiguration()
            {
                ConnectionString =
                    sensitiveData.GetConfigValue("root", nameof(LdapServerConfiguration.ConnectionString))!,
                UserName = RadiusAdapterConstants.AdminUserName,
                Password = RadiusAdapterConstants.AdminUserPassword
            }
        };
        
        var rootConfig = CreateRadiusConfiguration(sensitiveData, ldapServersSection);
        
        await StartHostAsync(
            rootConfig,
            configure: hostConfiguration);
        
        // AccessRequest step 1
        var accessRequest = CreateRadiusPacket(PacketCode.AccessRequest);
        accessRequest.AddAttributeValue("NAS-Identifier", RadiusAdapterConstants.DefaultNasIdentifier);
        accessRequest.AddAttributeValue("User-Name", RadiusAdapterConstants.BindUserName);
        accessRequest.AddAttributeValue("User-Password", RadiusAdapterConstants.BindUserPassword);
        
        //Should check groups
        accessRequest.AddAttributeValue("Acct-Authentic", (uint)AccountType.Domain);
        
        var response = SendPacketAsync(accessRequest);

        Assert.NotNull(response);
        Assert.Single(mfAPiMock.Invocations);
        Assert.Equal(PacketCode.AccessAccept, response.Code);
        Assert.False(response.Attributes.ContainsKey("State"));
    }
    
    [Theory]
    [InlineData("none-root-conf.env", AccountType.Microsoft)]
    [InlineData("none-root-conf.env", AccountType.Local)]
    [InlineData("ad-root-conf.env", AccountType.Microsoft)]
    [InlineData("ad-root-conf.env", AccountType.Local)]
    [InlineData("radius-root-conf.env", AccountType.Microsoft)]
    [InlineData("radius-root-conf.env", AccountType.Local)]
    public async Task BST021_NotDomainUser_ShouldAccept(string configName, AccountType accountType)
    {
        var state = "BST021_ShouldAccept";
        
        var sensitiveData =
            E2ETestsUtils.GetConfigSensitiveData(configName);
        
        var mfAPiMock = new Mock<IMultifactorApiService>();
        
        mfAPiMock
            .Setup(x => x.CreateSecondFactorRequestAsync(It.IsAny<CreateSecondFactorRequest>()))
            .ReturnsAsync(new MultifactorResponse(AuthenticationStatus.Accept, state));
        
        var hostConfiguration = (HostApplicationBuilder builder) =>
        {
            builder.Services.ReplaceService(mfAPiMock.Object);
        };

        var ldapServersSection = new LdapServersSection()
        {
            LdapServer = new LdapServerConfiguration()
            {
                ConnectionString =
                    sensitiveData.GetConfigValue("root", nameof(LdapServerConfiguration.ConnectionString))!,
                UserName = RadiusAdapterConstants.AdminUserName,
                Password = RadiusAdapterConstants.AdminUserPassword
            }
        };
        
        var rootConfig = CreateRadiusConfiguration(sensitiveData, ldapServersSection);
        
        await StartHostAsync(
            rootConfig,
            configure: hostConfiguration);
        
        // AccessRequest step 1
        var accessRequest = CreateRadiusPacket(PacketCode.AccessRequest);
        accessRequest.AddAttributeValue("NAS-Identifier", RadiusAdapterConstants.DefaultNasIdentifier);
        accessRequest.AddAttributeValue("User-Name", RadiusAdapterConstants.BindUserName);
        accessRequest.AddAttributeValue("User-Password", RadiusAdapterConstants.BindUserPassword);
        
        //Should not check groups
        accessRequest.AddAttributeValue("Acct-Authentic", (uint)accountType);
        
        var response = SendPacketAsync(accessRequest);

        Assert.NotNull(response);
        Assert.Single(mfAPiMock.Invocations);
        Assert.Equal(PacketCode.AccessAccept, response.Code);
        Assert.False(response.Attributes.ContainsKey("State"));
    }

    [Theory]
    [InlineData("none-root-conf.env")]
    [InlineData("ad-root-conf.env")]
    [InlineData("radius-root-conf.env")]
    public async Task BST023_ShouldAccept(string configName)
    {
        var challenge1 = "challenge-1";
        var state = "BST023_ShouldAccept";

        var sensitiveData =
            E2ETestsUtils.GetConfigSensitiveData(configName);

        var mfAPiMock = new Mock<IMultifactorApiService>();

        mfAPiMock
            .Setup(x => x.CreateSecondFactorRequestAsync(It.IsAny<CreateSecondFactorRequest>()))
            .ReturnsAsync(new MultifactorResponse(AuthenticationStatus.Awaiting, state));

        mfAPiMock
            .Setup(x => x.SendChallengeAsync(It.Is<SendChallengeRequest>(r => r.Answer == challenge1)))
            .ReturnsAsync(new MultifactorResponse(AuthenticationStatus.Accept));

        var hostConfiguration = (HostApplicationBuilder builder) =>
        {
            builder.Services.ReplaceService(mfAPiMock.Object);
        };
        
        var ldapServersSection = new LdapServersSection()
        {
            LdapServer = new LdapServerConfiguration()
            {
                ConnectionString =
                    sensitiveData.GetConfigValue("root", nameof(LdapServerConfiguration.ConnectionString))!,
                UserName = RadiusAdapterConstants.AdminUserName,
                Password = RadiusAdapterConstants.AdminUserPassword
            }
        };
        
        var rootConfig = CreateRadiusConfiguration(sensitiveData, ldapServersSection);

        await StartHostAsync(
            rootConfig,
            configure: hostConfiguration);
        
        // AccessRequest step 1
        var accessRequest = CreateRadiusPacket(PacketCode.AccessRequest, identifier: 0);
        accessRequest.AddAttributeValue("NAS-Identifier", RadiusAdapterConstants.DefaultNasIdentifier);
        accessRequest.AddAttributeValue("User-Name", RadiusAdapterConstants.BindUserName);
        accessRequest.AddAttributeValue("User-Password", RadiusAdapterConstants.BindUserPassword);

        var response = SendPacketAsync(accessRequest);

        Assert.NotNull(response);
        Assert.Single(mfAPiMock.Invocations);
        Assert.Equal(PacketCode.AccessChallenge, response.Code);
        var attribute = response.Attributes["State"];
        Assert.NotNull(attribute.Values.FirstOrDefault());
        var attributeValue = attribute.Values.FirstOrDefault();
        var responseState =  Encoding.UTF8.GetString(attributeValue as byte[]);
        Assert.Equal(responseState, state);

        // Challenge step 2
        accessRequest = CreateRadiusPacket(PacketCode.AccessRequest, identifier: 1);
        accessRequest.AddAttributeValue("NAS-Identifier", RadiusAdapterConstants.DefaultNasIdentifier);
        accessRequest.AddAttributeValue("User-Name", RadiusAdapterConstants.BindUserName);
        accessRequest.AddAttributeValue("State", state);
        accessRequest.AddAttributeValue("User-Password", challenge1);

        response = SendPacketAsync(accessRequest);

        Assert.NotNull(response);
        Assert.Equal(2, mfAPiMock.Invocations.Count);
        Assert.Equal(PacketCode.AccessAccept, response.Code);
    }

    [Theory]
    [InlineData("none-root-conf.env")]
    [InlineData("ad-root-conf.env")]
    [InlineData("radius-root-conf.env")]
    public async Task BST024_ShouldAccept(string configName)
    {
        var state = "BST018_ShouldAccept";
        var challenge1 = "challenge-1";
        var challenge2 = "challenge-2";
        
        var sensitiveData =
            E2ETestsUtils.GetConfigSensitiveData(configName);
        
        var mfAPiMock = new Mock<IMultifactorApiService>();
        
        mfAPiMock
            .Setup(x => x.CreateSecondFactorRequestAsync(It.IsAny<CreateSecondFactorRequest>()))
            .ReturnsAsync(new MultifactorResponse(AuthenticationStatus.Awaiting, state));
        
        mfAPiMock
            .Setup(x => x.SendChallengeAsync(It.Is<SendChallengeRequest>(r => r.Answer == challenge1)))
            .ReturnsAsync(new MultifactorResponse(AuthenticationStatus.Awaiting));
        
        mfAPiMock
            .Setup(x => x.SendChallengeAsync(It.Is<SendChallengeRequest>(r => r.Answer == challenge2)))
            .ReturnsAsync(new MultifactorResponse(AuthenticationStatus.Accept));
        
        var hostConfiguration = (HostApplicationBuilder builder) =>
        {
            builder.Services.ReplaceService(mfAPiMock.Object);
        };

        var ldapServersSection = new LdapServersSection()
        {
            LdapServer = new LdapServerConfiguration()
            {
                ConnectionString =
                    sensitiveData.GetConfigValue("root", nameof(LdapServerConfiguration.ConnectionString))!,
                UserName = RadiusAdapterConstants.AdminUserName,
                Password = RadiusAdapterConstants.AdminUserPassword
            }
        };
        
        var rootConfig = CreateRadiusConfiguration(sensitiveData, ldapServersSection);
        
        await StartHostAsync(
            rootConfig,
            configure: hostConfiguration);
        
        // AccessRequest step 1
        var accessRequest = CreateRadiusPacket(PacketCode.AccessRequest, identifier: 0);
        accessRequest.AddAttributeValue("NAS-Identifier", RadiusAdapterConstants.DefaultNasIdentifier);
        accessRequest.AddAttributeValue("User-Name", RadiusAdapterConstants.BindUserName);
        accessRequest.AddAttributeValue("User-Password", RadiusAdapterConstants.BindUserPassword);
        
        var response = SendPacketAsync(accessRequest);
        
        Assert.NotNull(response);
        Assert.Single(mfAPiMock.Invocations);
        Assert.Equal(PacketCode.AccessChallenge, response.Code);
        var attribute = response.Attributes["State"];
        Assert.NotNull(attribute.Values.FirstOrDefault());
        var attributeValue = attribute.Values.FirstOrDefault();
        var responseState =  Encoding.UTF8.GetString(attributeValue as byte[]);
        Assert.Equal(responseState, state);
        
        // Challenge step 2
        accessRequest = CreateRadiusPacket(PacketCode.AccessRequest, identifier: 1);
        accessRequest.AddAttributeValue("NAS-Identifier", RadiusAdapterConstants.DefaultNasIdentifier);
        accessRequest.AddAttributeValue("User-Name", RadiusAdapterConstants.BindUserName);
        accessRequest.AddAttributeValue("State", state);
        accessRequest.AddAttributeValue("User-Password", challenge1);
        
        response = SendPacketAsync(accessRequest);
        
        Assert.NotNull(response);
        Assert.Equal(2, mfAPiMock.Invocations.Count);
        Assert.Equal(PacketCode.AccessChallenge, response.Code);
        
        // Challenge step 3
        accessRequest = CreateRadiusPacket(PacketCode.AccessRequest, identifier: 2);
        accessRequest.AddAttributeValue("NAS-Identifier", RadiusAdapterConstants.DefaultNasIdentifier);
        accessRequest.AddAttributeValue("User-Name", RadiusAdapterConstants.BindUserName);
        accessRequest.AddAttributeValue("State", state);
        accessRequest.AddAttributeValue("User-Password", challenge2);
        
        response = SendPacketAsync(accessRequest);
        
        Assert.NotNull(response);
        Assert.Equal(3, mfAPiMock.Invocations.Count);
        Assert.Equal(PacketCode.AccessAccept, response.Code);
    }
    
    [Theory]
    [InlineData("no-ldap-radius-conf.env")]
    [InlineData("no-ldap-none-conf.env")]
    public async Task PreAuth_NoLdapServerSettings_ShouldAccept(string configName)
    {
        var state = "PreAuth_NoLdapServerSettings";
        var challenge1 = "challenge-1";
        var challenge2 = "challenge-2";
        
        var sensitiveData =
            E2ETestsUtils.GetConfigSensitiveData(configName);
        
        var mfAPiMock = new Mock<IMultifactorApiService>();
        
        mfAPiMock
            .Setup(x => x.CreateSecondFactorRequestAsync(It.IsAny<CreateSecondFactorRequest>()))
            .ReturnsAsync(new MultifactorResponse(AuthenticationStatus.Awaiting, state));
        
        mfAPiMock
            .Setup(x => x.SendChallengeAsync(It.Is<SendChallengeRequest>(r => r.Answer == challenge1)))
            .ReturnsAsync(new MultifactorResponse(AuthenticationStatus.Awaiting));
        
        mfAPiMock
            .Setup(x => x.SendChallengeAsync(It.Is<SendChallengeRequest>(r => r.Answer == challenge2)))
            .ReturnsAsync(new MultifactorResponse(AuthenticationStatus.Accept));
        
        var hostConfiguration = (HostApplicationBuilder builder) =>
        {
            builder.Services.ReplaceService(mfAPiMock.Object);
        };

        var rootConfig = CreateRadiusConfiguration(sensitiveData, new());
        
        await StartHostAsync(
            rootConfig,
            configure: hostConfiguration);
        
        // AccessRequest step 1
        var accessRequest = CreateRadiusPacket(PacketCode.AccessRequest, identifier: 0);
        accessRequest.AddAttributeValue("NAS-Identifier", RadiusAdapterConstants.DefaultNasIdentifier);
        accessRequest.AddAttributeValue("User-Name", RadiusAdapterConstants.BindUserName);
        accessRequest.AddAttributeValue("User-Password", RadiusAdapterConstants.BindUserPassword);
        
        var response = SendPacketAsync(accessRequest);
        
        Assert.NotNull(response);
        Assert.Single(mfAPiMock.Invocations);
        Assert.Equal(PacketCode.AccessChallenge, response.Code);
        var attribute = response.Attributes["State"];
        Assert.NotNull(attribute.Values.FirstOrDefault());
        var attributeValue = attribute.Values.FirstOrDefault();
        var responseState =  Encoding.UTF8.GetString(attributeValue as byte[]);
        Assert.Equal(responseState, state);
        
        // Challenge step 2
        accessRequest = CreateRadiusPacket(PacketCode.AccessRequest, identifier: 1);
        accessRequest.AddAttributeValue("NAS-Identifier", RadiusAdapterConstants.DefaultNasIdentifier);
        accessRequest.AddAttributeValue("User-Name", RadiusAdapterConstants.BindUserName);
        accessRequest.AddAttributeValue("State", state);
        accessRequest.AddAttributeValue("User-Password", challenge1);
        
        response = SendPacketAsync(accessRequest);
        
        Assert.NotNull(response);
        Assert.Equal(2, mfAPiMock.Invocations.Count);
        Assert.Equal(PacketCode.AccessChallenge, response.Code);
        
        // Challenge step 3
        accessRequest = CreateRadiusPacket(PacketCode.AccessRequest, identifier: 2);
        accessRequest.AddAttributeValue("NAS-Identifier", RadiusAdapterConstants.DefaultNasIdentifier);
        accessRequest.AddAttributeValue("User-Name", RadiusAdapterConstants.BindUserName);
        accessRequest.AddAttributeValue("State", state);
        accessRequest.AddAttributeValue("User-Password", challenge2);
        
        response = SendPacketAsync(accessRequest);
        
        Assert.NotNull(response);
        Assert.Equal(3, mfAPiMock.Invocations.Count);
        Assert.Equal(PacketCode.AccessAccept, response.Code);
    }

    private RadiusAdapterConfiguration CreateRadiusConfiguration(ConfigSensitiveData[] sensitiveData, LdapServersSection ldapServersSection)
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

                PreAuthenticationMethod = "any",
                InvalidCredentialDelay = "3",

                NpsServerEndpoint = sensitiveData.GetConfigValue(
                    configName,
                    nameof(AppSettingsSection.NpsServerEndpoint))!,
                
                NpsServerTimeout = "00:00:10",

                AdapterClientEndpoint = sensitiveData.GetConfigValue(
                    configName,
                    nameof(AppSettingsSection.AdapterClientEndpoint))!,

                FirstFactorAuthenticationSource = sensitiveData.GetConfigValue(
                    configName,
                    nameof(AppSettingsSection.FirstFactorAuthenticationSource))!
            },
            
            LdapServers = ldapServersSection
        };

        return rootConfig;
    }
}