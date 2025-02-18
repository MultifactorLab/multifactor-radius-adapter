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
public class MultipleActiveDirectoryGroupsTests(RadiusFixtures radiusFixtures) : E2ETestBase(radiusFixtures)
{
    [Theory]
    [InlineData("ad-root-conf.env")]
    public async Task BST009_ShouldAccept(string configName)
    {
        var sensitiveData =
            E2ETestsUtils.GetEnvironmentVariables(configName);

        var prefix = E2ETestsUtils.GetEnvPrefix(sensitiveData.First().Key);

        var secondFactorMock = new Mock<IMultifactorApiAdapter>();

        secondFactorMock
            .Setup(x => x.CreateSecondFactorRequestAsync(It.IsAny<RadiusContext>()))
            .ReturnsAsync(new SecondFactorResponse(AuthenticationCode.Accept));

        var hostConfiguration = (RadiusHostApplicationBuilder builder) =>
        {
            builder.Services.ReplaceService(secondFactorMock.Object);
        };

        await TestEnvironmentVariables.With(async env =>
        {
            env.SetEnvironmentVariables(sensitiveData);

            await StartHostAsync(
                "root-multiple-active-directory-groups.config",
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
            Assert.Single(secondFactorMock.Invocations);
            Assert.Equal(PacketCode.AccessAccept, response.Header.Code);
        });
    }
    
    [Theory]
    [InlineData("ad-root-conf.env")]
    public async Task BST010_ShouldAccept(string configName)
    {
        var sensitiveData =
            E2ETestsUtils.GetEnvironmentVariables(configName);

        var prefix = E2ETestsUtils.GetEnvPrefix(sensitiveData.First().Key);

        var secondFactorMock = new Mock<IMultifactorApiAdapter>();

        secondFactorMock
            .Setup(x => x.CreateSecondFactorRequestAsync(It.IsAny<RadiusContext>()))
            .ReturnsAsync(new SecondFactorResponse(AuthenticationCode.Accept));

        var hostConfiguration = (RadiusHostApplicationBuilder builder) =>
        {
            builder.Services.ReplaceService(secondFactorMock.Object);
        };

        await TestEnvironmentVariables.With(async env =>
        {
            env.SetEnvironmentVariables(sensitiveData);

            await StartHostAsync(
                "root-multiple-not-existed-active-directory-groups.config",
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
            Assert.Empty(secondFactorMock.Invocations);
            Assert.Equal(PacketCode.AccessReject, response.Header.Code);
        });
    }
}