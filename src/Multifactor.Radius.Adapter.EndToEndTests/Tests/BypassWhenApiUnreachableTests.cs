using Moq;
using MultiFactor.Radius.Adapter.Core.Framework;
using MultiFactor.Radius.Adapter.Core.Framework.Context;
using MultiFactor.Radius.Adapter.Core.Radius;
using Multifactor.Radius.Adapter.EndToEndTests.Constants;
using Multifactor.Radius.Adapter.EndToEndTests.Fixtures;
using MultiFactor.Radius.Adapter.Infrastructure.Http;
using MultiFactor.Radius.Adapter.Services.MultiFactorApi;
using MultiFactor.Radius.Adapter.Services.MultiFactorApi.Dto;
using MultiFactor.Radius.Adapter.Services.MultiFactorApi.Models;

namespace Multifactor.Radius.Adapter.EndToEndTests.Tests;

[Collection("Radius e2e")]
public class BypassWhenApiUnreachableTests(RadiusFixtures radiusFixtures) : E2ETestBase(radiusFixtures)
{
    [Fact]
    public async Task BST001_ShouldAccept()
    {
        var secondFactorMock = new Mock<IMultifactorApiAdapter>();

        secondFactorMock
            .Setup(x => x.CreateSecondFactorRequestAsync(It.IsAny<RadiusContext>()))
            .ReturnsAsync(new SecondFactorResponse(AuthenticationCode.Accept));

        var hostConfiguration = (RadiusHostApplicationBuilder builder) =>
        {
            builder.Services.ReplaceService(secondFactorMock.Object);
        };

        await StartHostAsync(
            "root-no-bypass-when-api-unreachable.config",
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
        Assert.Equal(PacketCode.AccessAccept, response.Header.Code);
    }

    [Fact]
    public async Task BST002_ShouldAccept()
    {
        var secondFactorMock = new Mock<IMultifactorApiAdapter>();

        secondFactorMock
            .Setup(x => x.CreateSecondFactorRequestAsync(It.IsAny<RadiusContext>()))
            .ReturnsAsync(new SecondFactorResponse(AuthenticationCode.Accept));

        var hostConfiguration = (RadiusHostApplicationBuilder builder) =>
        {
            builder.Services.ReplaceService(secondFactorMock.Object);
        };


        await StartHostAsync(
            "root-bypass-true-when-api-unreachable.config",
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
        Assert.Equal(PacketCode.AccessAccept, response.Header.Code);
    }

    [Fact]
    public async Task BST003_ShouldAccept()
    {
        var secondFactorMock = new Mock<IMultifactorApiClient>();

        secondFactorMock
            .Setup(x => x.CreateRequestAsync(It.IsAny<CreateRequestDto>(), It.IsAny<BasicAuthHeaderValue>()))
            .ThrowsAsync(new MultifactorApiUnreachableException());

        var hostConfiguration = (RadiusHostApplicationBuilder builder) =>
        {
            builder.Services.ReplaceService(secondFactorMock.Object);
        };


        await StartHostAsync(
            "root-no-bypass-when-api-unreachable.config",
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
        Assert.Equal(PacketCode.AccessAccept, response.Header.Code);
    }

    [Fact]
    public async Task BST004_ShouldReject()
    {
        var secondFactorMock = new Mock<IMultifactorApiClient>();

        secondFactorMock
            .Setup(x => x.CreateRequestAsync(It.IsAny<CreateRequestDto>(), It.IsAny<BasicAuthHeaderValue>()))
            .ThrowsAsync(new MultifactorApiUnreachableException());

        var hostConfiguration = (RadiusHostApplicationBuilder builder) =>
        {
            builder.Services.ReplaceService(secondFactorMock.Object);
        };

        await StartHostAsync(
            "root-bypass-false-when-api-unreachable.config",
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
        Assert.Equal(PacketCode.AccessReject, response.Header.Code);
    }

    [Fact]
    public async Task BST005_ShouldAccept()
    {
        var secondFactorMock = new Mock<IMultifactorApiClient>();

        secondFactorMock
            .Setup(x => x.CreateRequestAsync(It.IsAny<CreateRequestDto>(), It.IsAny<BasicAuthHeaderValue>()))
            .ThrowsAsync(new MultifactorApiUnreachableException());

        var hostConfiguration = (RadiusHostApplicationBuilder builder) =>
        {
            builder.Services.ReplaceService(secondFactorMock.Object);
        };


        await StartHostAsync(
            "root-bypass-true-when-api-unreachable.config",
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
        Assert.Equal(PacketCode.AccessAccept, response.Header.Code);
    }

    [Fact]
    public async Task BST006_ShouldAccept()
    {
        var secondFactorMock = new Mock<IMultifactorApiAdapter>();

        secondFactorMock
            .Setup(x => x.CreateSecondFactorRequestAsync(It.IsAny<RadiusContext>()))
            .ReturnsAsync(new SecondFactorResponse(AuthenticationCode.Accept));

        var hostConfiguration = (RadiusHostApplicationBuilder builder) =>
        {
            builder.Services.ReplaceService(secondFactorMock.Object);
        };

        await StartHostAsync(
            "root-bypass-false-when-api-unreachable.config",
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
        Assert.Equal(PacketCode.AccessAccept, response.Header.Code);
    }
}