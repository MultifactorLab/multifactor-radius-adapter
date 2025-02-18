using Moq;
using MultiFactor.Radius.Adapter.Core.Framework;
using MultiFactor.Radius.Adapter.Core.Framework.Context;
using MultiFactor.Radius.Adapter.Core.Radius;
using Multifactor.Radius.Adapter.EndToEndTests.Constants;
using Multifactor.Radius.Adapter.EndToEndTests.Fixtures;
using MultiFactor.Radius.Adapter.Services.MultiFactorApi;
using MultiFactor.Radius.Adapter.Services.MultiFactorApi.Models;

namespace Multifactor.Radius.Adapter.EndToEndTests.Tests;

[Collection("Radius e2e")]
public class FirstFactorTests(RadiusFixtures radiusFixtures) : E2ETestBase(radiusFixtures)
{
    [Theory]
    [InlineData("ad-root-conf.env")]
    [InlineData("radius-root-conf.env")]
    public async Task BST016_ShouldAccept(string configName)
    {
        var sensitiveData =
            E2ETestsUtils.GetEnvironmentVariables(configName);

        var prefix = E2ETestsUtils.GetEnvPrefix(sensitiveData.First().Key);

        var mfAPiMock = new Mock<IMultifactorApiAdapter>();

        mfAPiMock
            .Setup(x => x.CreateSecondFactorRequestAsync(It.IsAny<RadiusContext>()))
            .ReturnsAsync(new SecondFactorResponse(AuthenticationCode.Accept));

        var hostConfiguration = (RadiusHostApplicationBuilder builder) =>
        {
            builder.Services.ReplaceService(mfAPiMock.Object);
        };
        
        await TestEnvironmentVariables.With(async env =>
        {
            env.SetEnvironmentVariables(sensitiveData);

            await StartHostAsync(
                "root-first-factor.config",
                envPrefix: prefix,
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
        });
    }
    
    [Theory]
    [InlineData("ad-root-conf.env")]
    [InlineData("radius-root-conf.env")]
    public async Task BST017_ShouldAccept(string configName)
    {
        var sensitiveData =
            E2ETestsUtils.GetEnvironmentVariables(configName);

        var prefix = E2ETestsUtils.GetEnvPrefix(sensitiveData.First().Key);

        var mfApiMock = new Mock<IMultifactorApiAdapter>();

        mfApiMock
            .Setup(x => x.CreateSecondFactorRequestAsync(It.IsAny<RadiusContext>()))
            .ReturnsAsync(new SecondFactorResponse(AuthenticationCode.Accept));

        var hostConfiguration = (RadiusHostApplicationBuilder builder) =>
        {
            builder.Services.ReplaceService(mfApiMock.Object);
        };
        
        await TestEnvironmentVariables.With(async env =>
        {
            env.SetEnvironmentVariables(sensitiveData);

            await StartHostAsync(
                "root-first-factor.config",
                envPrefix: prefix,
                configure: hostConfiguration);

            var accessRequest = CreateRadiusPacket(PacketCode.AccessRequest);
            accessRequest!.AddAttributes(new Dictionary<string, object>()
            {
                { "NAS-Identifier", RadiusAdapterConstants.DefaultNasIdentifier },
                { "User-Name", RadiusAdapterConstants.BindUserName },
                { "User-Password", "Bad-Password" }
            });

            var response = SendPacketAsync(accessRequest);

            Assert.NotNull(response);
            Assert.Empty(mfApiMock.Invocations);
            Assert.Equal(PacketCode.AccessReject, response.Header.Code);
        });
    }
}