using Moq;
using MultiFactor.Radius.Adapter.Core.Framework;
using MultiFactor.Radius.Adapter.Core.Framework.Context;
using MultiFactor.Radius.Adapter.Core.Radius;
using MultiFactor.Radius.Adapter.Infrastructure.Http;
using MultiFactor.Radius.Adapter.Services.MultiFactorApi;
using MultiFactor.Radius.Adapter.Services.MultiFactorApi.Dto;
using MultiFactor.Radius.Adapter.Services.MultiFactorApi.Models;
using MultiFactor.Radius.Adapter.Tests.E2E.Constants;
using MultiFactor.Radius.Adapter.Tests.Fixtures;

namespace MultiFactor.Radius.Adapter.Tests.E2E.Tests;

[Collection("Radius e2e")]
public class AccessRequestTests : E2ETestBase
{
    public AccessRequestTests(RadiusFixtures radiusFixtures) : base(radiusFixtures)
    {
    }

    [Fact]
    public async Task SendAuthRequestWithoutCredentials_ShouldReject()
    {
        await StartHostAsync(RadiusAdapterConfigs.RootConfig, new[] { RadiusAdapterConfigs.AccessRequestConfig });

        var accessRequest = CreateRadiusPacket(PacketCode.AccessRequest);
        accessRequest.AddAttributes(new Dictionary<string, object>()
            { { "NAS-Identifier", RadiusAdapterConstants.DefaultNasIdentifier } });

        var response = SendPacketAsync(accessRequest);

        Assert.NotNull(response);
        Assert.Equal(PacketCode.AccessReject, response.Header.Code);
    }
    
    [Theory]
    [InlineData("ad.env")]
    public async Task SendAuthRequestWithBindUser_ShouldAccept(string configName)
    {
        var sensitiveData =
            E2ETestsUtils.GetSensitiveData(configName);
        
        var prefix = E2ETestsUtils.GetEnvPrefix(sensitiveData.First().Key);
        
        await TestEnvironmentVariables.With(async env =>
        {
            env.SetEnvironmentVariables(sensitiveData);
            
            await StartHostAsync(
                RadiusAdapterConfigs.RootConfig,
                new[] { RadiusAdapterConfigs.AccessRequestConfig },
                envPrefix: prefix);

            var accessRequest = CreateRadiusPacket(PacketCode.AccessRequest);
            accessRequest.AddAttributes(new Dictionary<string, object>()
            {
                { "NAS-Identifier", RadiusAdapterConstants.DefaultNasIdentifier },
                { "User-Name", RadiusAdapterConstants.BindUserName },
                { "User-Password", RadiusAdapterConstants.BindUserPassword }
            });

            var response = SendPacketAsync(accessRequest);

            Assert.NotNull(response);
            Assert.Equal(PacketCode.AccessAccept, response.Header.Code);
        });
    }
    
    [Theory]
    [InlineData("ad.env")]
    public async Task SendAuthRequestWithAdminUser_ShouldAccept(string configName)
    {
        var sensitiveData =
            E2ETestsUtils.GetSensitiveData(configName);
        
        var prefix = E2ETestsUtils.GetEnvPrefix(sensitiveData.First().Key);
        
        await TestEnvironmentVariables.With(async env =>
        {
            env.SetEnvironmentVariables(sensitiveData);
            
            await StartHostAsync(
                RadiusAdapterConfigs.RootConfig,
                new[] { RadiusAdapterConfigs.AccessRequestConfig },
                envPrefix: prefix);

            var accessRequest = CreateRadiusPacket(PacketCode.AccessRequest);
            accessRequest.AddAttributes(new Dictionary<string, object>()
            {
                { "NAS-Identifier", RadiusAdapterConstants.DefaultNasIdentifier },
                { "User-Name", RadiusAdapterConstants.AdminUserName },
                { "User-Password", RadiusAdapterConstants.AdminUserPassword }
            });

            var response = SendPacketAsync(accessRequest);

            Assert.NotNull(response);
            Assert.Equal(PacketCode.AccessAccept, response.Header.Code);
        });
    }
    
    [Theory]
    [InlineData("ad.env")]
    public async Task SendAuthRequestWithPasswordUser_ShouldAccept(string configName)
    {
        var sensitiveData =
            E2ETestsUtils.GetSensitiveData(configName);
        
        var prefix = E2ETestsUtils.GetEnvPrefix(sensitiveData.First().Key);
        
        await TestEnvironmentVariables.With(async env =>
        {
            env.SetEnvironmentVariables(sensitiveData);
            
            await StartHostAsync(
                RadiusAdapterConfigs.RootConfig,
                new[] { RadiusAdapterConfigs.AccessRequestConfig },
                envPrefix: prefix);

            var accessRequest = CreateRadiusPacket(PacketCode.AccessRequest);
            accessRequest.AddAttributes(new Dictionary<string, object>()
            {
                { "NAS-Identifier", RadiusAdapterConstants.DefaultNasIdentifier },
                { "User-Name", RadiusAdapterConstants.ChangePasswordUserName },
                { "User-Password", RadiusAdapterConstants.ChangePasswordUserPassword }
            });

            var response = SendPacketAsync(accessRequest);

            Assert.NotNull(response);
            Assert.Equal(PacketCode.AccessAccept, response.Header.Code);
        });
    }
    
    [Theory]
    [InlineData("ad-root-conf.env")]
    public async Task BST001_ShouldAccept(string configName)
    {
        var sensitiveData =
            E2ETestsUtils.GetSensitiveData(configName);
        
        var prefix = E2ETestsUtils.GetEnvPrefix(sensitiveData.First().Key);

        var secondFactorMock = new Mock<IMultifactorApiAdapter>();
        
        secondFactorMock
            .Setup(x => x.CreateSecondFactorRequestAsync(It.IsAny<RadiusContext>()))
            .ReturnsAsync(new SecondFactorResponse(AuthenticationCode.Accept));
        
        var hostConfiguration = (RadiusHostApplicationBuilder builder) =>
        {
            builder.Services.ReplaceService<IMultifactorApiAdapter>(secondFactorMock.Object);
        };
        
        await TestEnvironmentVariables.With(async env =>
        {
            env.SetEnvironmentVariables(sensitiveData);
            
            await StartHostAsync(
                "root-no-bypass-when-api-unreachable.config",
                envPrefix: prefix,
                configure: hostConfiguration);

            var accessRequest = CreateRadiusPacket(PacketCode.AccessRequest);
            accessRequest.AddAttributes(new Dictionary<string, object>()
            {
                { "NAS-Identifier", RadiusAdapterConstants.DefaultNasIdentifier },
                { "User-Name", RadiusAdapterConstants.BindUserName },
                { "User-Password", RadiusAdapterConstants.BindUserPassword }
            });

            var response = SendPacketAsync(accessRequest);

            Assert.NotNull(response);
            Assert.Equal(PacketCode.AccessAccept, response.Header.Code);
        });
    }
    
    [Theory]
    [InlineData("ad-root-conf.env")]
    public async Task BST002_ShouldAccept(string configName)
    {
        var sensitiveData =
            E2ETestsUtils.GetSensitiveData(configName);
        
        var prefix = E2ETestsUtils.GetEnvPrefix(sensitiveData.First().Key);

        var secondFactorMock = new Mock<IMultifactorApiAdapter>();
        
        secondFactorMock
            .Setup(x => x.CreateSecondFactorRequestAsync(It.IsAny<RadiusContext>()))
            .ReturnsAsync(new SecondFactorResponse(AuthenticationCode.Accept));
        
        var hostConfiguration = (RadiusHostApplicationBuilder builder) =>
        {
            builder.Services.ReplaceService<IMultifactorApiAdapter>(secondFactorMock.Object);
        };
        
        await TestEnvironmentVariables.With(async env =>
        {
            env.SetEnvironmentVariables(sensitiveData);
            
            await StartHostAsync(
                "root-bypass-true-when-api-unreachable.config",
                envPrefix: prefix,
                configure: hostConfiguration);

            var accessRequest = CreateRadiusPacket(PacketCode.AccessRequest);
            accessRequest.AddAttributes(new Dictionary<string, object>()
            {
                { "NAS-Identifier", RadiusAdapterConstants.DefaultNasIdentifier },
                { "User-Name", RadiusAdapterConstants.BindUserName },
                { "User-Password", RadiusAdapterConstants.BindUserPassword }
            });

            var response = SendPacketAsync(accessRequest);

            Assert.NotNull(response);
            Assert.Equal(PacketCode.AccessAccept, response.Header.Code);
        });
    }
    
    [Theory]
    [InlineData("ad-root-conf.env")]
    public async Task BST003_ShouldAccept(string configName)
    {
        var sensitiveData =
            E2ETestsUtils.GetSensitiveData(configName);
        
        var prefix = E2ETestsUtils.GetEnvPrefix(sensitiveData.First().Key);

        var secondFactorMock = new Mock<IMultifactorApiClient>();
        
        secondFactorMock
            .Setup(x => x.CreateRequestAsync(It.IsAny<CreateRequestDto>(), It.IsAny<BasicAuthHeaderValue>()))
            .ThrowsAsync(new MultifactorApiUnreachableException());
        
        var hostConfiguration = (RadiusHostApplicationBuilder builder) =>
        {
            builder.Services.ReplaceService<IMultifactorApiClient>(secondFactorMock.Object);
        };
        
        await TestEnvironmentVariables.With(async env =>
        {
            env.SetEnvironmentVariables(sensitiveData);
            
            await StartHostAsync(
                "root-no-bypass-when-api-unreachable.config",
                envPrefix: prefix,
                configure: hostConfiguration);

            var accessRequest = CreateRadiusPacket(PacketCode.AccessRequest);
            accessRequest.AddAttributes(new Dictionary<string, object>()
            {
                { "NAS-Identifier", RadiusAdapterConstants.DefaultNasIdentifier },
                { "User-Name", RadiusAdapterConstants.BindUserName },
                { "User-Password", RadiusAdapterConstants.BindUserPassword }
            });

            var response = SendPacketAsync(accessRequest);

            Assert.NotNull(response);
            Assert.Equal(PacketCode.AccessAccept, response.Header.Code);
        });
    }
    
    [Theory]
    [InlineData("ad-root-conf.env")]
    public async Task BST004_ShouldReject(string configName)
    {
        var sensitiveData =
            E2ETestsUtils.GetSensitiveData(configName);
        
        var prefix = E2ETestsUtils.GetEnvPrefix(sensitiveData.First().Key);

        var secondFactorMock = new Mock<IMultifactorApiClient>();
        
        secondFactorMock
            .Setup(x => x.CreateRequestAsync(It.IsAny<CreateRequestDto>(), It.IsAny<BasicAuthHeaderValue>()))
            .ThrowsAsync(new MultifactorApiUnreachableException());
        
        var hostConfiguration = (RadiusHostApplicationBuilder builder) =>
        {
            builder.Services.ReplaceService<IMultifactorApiClient>(secondFactorMock.Object);
        };
        
        await TestEnvironmentVariables.With(async env =>
        {
            env.SetEnvironmentVariables(sensitiveData);
            
            await StartHostAsync(
                "root-bypass-false-when-api-unreachable.config",
                envPrefix: prefix,
                configure: hostConfiguration);

            var accessRequest = CreateRadiusPacket(PacketCode.AccessRequest);
            accessRequest.AddAttributes(new Dictionary<string, object>()
            {
                { "NAS-Identifier", RadiusAdapterConstants.DefaultNasIdentifier },
                { "User-Name", RadiusAdapterConstants.BindUserName },
                { "User-Password", RadiusAdapterConstants.BindUserPassword }
            });

            var response = SendPacketAsync(accessRequest);

            Assert.NotNull(response);
            Assert.Equal(PacketCode.AccessReject, response.Header.Code);
        });
    }
    
    [Theory]
    [InlineData("ad-root-conf.env")]
    public async Task BST005_ShouldAccept(string configName)
    {
        var sensitiveData =
            E2ETestsUtils.GetSensitiveData(configName);
        
        var prefix = E2ETestsUtils.GetEnvPrefix(sensitiveData.First().Key);

        var secondFactorMock = new Mock<IMultifactorApiClient>();
        
        secondFactorMock
            .Setup(x => x.CreateRequestAsync(It.IsAny<CreateRequestDto>(), It.IsAny<BasicAuthHeaderValue>()))
            .ThrowsAsync(new MultifactorApiUnreachableException());
        
        var hostConfiguration = (RadiusHostApplicationBuilder builder) =>
        {
            builder.Services.ReplaceService<IMultifactorApiClient>(secondFactorMock.Object);
        };
        
        await TestEnvironmentVariables.With(async env =>
        {
            env.SetEnvironmentVariables(sensitiveData);
            
            await StartHostAsync(
                "root-bypass-true-when-api-unreachable.config",
                envPrefix: prefix,
                configure: hostConfiguration);

            var accessRequest = CreateRadiusPacket(PacketCode.AccessRequest);
            accessRequest.AddAttributes(new Dictionary<string, object>()
            {
                { "NAS-Identifier", RadiusAdapterConstants.DefaultNasIdentifier },
                { "User-Name", RadiusAdapterConstants.BindUserName },
                { "User-Password", RadiusAdapterConstants.BindUserPassword }
            });

            var response = SendPacketAsync(accessRequest);

            Assert.NotNull(response);
            Assert.Equal(PacketCode.AccessAccept, response.Header.Code);
        });
    }
    
    [Theory]
    [InlineData("ad-root-conf.env")]
    public async Task BST006_ShouldAccept(string configName)
    {
        var sensitiveData =
            E2ETestsUtils.GetSensitiveData(configName);
        
        var prefix = E2ETestsUtils.GetEnvPrefix(sensitiveData.First().Key);

        var secondFactorMock = new Mock<IMultifactorApiAdapter>();
        
        secondFactorMock
            .Setup(x => x.CreateSecondFactorRequestAsync(It.IsAny<RadiusContext>()))
            .ReturnsAsync(new SecondFactorResponse(AuthenticationCode.Accept));
        
        var hostConfiguration = (RadiusHostApplicationBuilder builder) =>
        {
            builder.Services.ReplaceService<IMultifactorApiAdapter>(secondFactorMock.Object);
        };
        
        await TestEnvironmentVariables.With(async env =>
        {
            env.SetEnvironmentVariables(sensitiveData);
            
            await StartHostAsync(
                "root-bypass-false-when-api-unreachable.config",
                envPrefix: prefix,
                configure: hostConfiguration);

            var accessRequest = CreateRadiusPacket(PacketCode.AccessRequest);
            accessRequest.AddAttributes(new Dictionary<string, object>()
            {
                { "NAS-Identifier", RadiusAdapterConstants.DefaultNasIdentifier },
                { "User-Name", RadiusAdapterConstants.BindUserName },
                { "User-Password", RadiusAdapterConstants.BindUserPassword }
            });

            var response = SendPacketAsync(accessRequest);

            Assert.NotNull(response);
            Assert.Equal(PacketCode.AccessAccept, response.Header.Code);
        });
    }
}