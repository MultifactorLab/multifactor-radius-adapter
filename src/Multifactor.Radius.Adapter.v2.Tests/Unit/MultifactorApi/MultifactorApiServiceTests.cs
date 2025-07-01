using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Multifactor.Core.Ldap.Attributes;
using Multifactor.Radius.Adapter.v2.Core;
using Multifactor.Radius.Adapter.v2.Core.Auth;
using Multifactor.Radius.Adapter.v2.Core.Auth.PreAuthMode;
using Multifactor.Radius.Adapter.v2.Core.Ldap;
using Multifactor.Radius.Adapter.v2.Core.MultifactorApi;
using Multifactor.Radius.Adapter.v2.Core.MultifactorApi.PrivacyMode;
using Multifactor.Radius.Adapter.v2.Exceptions;
using Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline.Context;
using Multifactor.Radius.Adapter.v2.Services.AuthenticatedClientCache;
using Multifactor.Radius.Adapter.v2.Services.MultifactorApi;
using Multifactor.Radius.Adapter.v2.Tests.Fixture;

namespace Multifactor.Radius.Adapter.v2.Tests.Unit.MultifactorApi;

public class MultifactorApiServiceTests
{
    [Fact]
    public async Task CreateSecondFactorRequestAsync_EmptyContext_ShouldThrow()
    {
        var apiMock = new Mock<IMultifactorApi>();
        var cacheMock = new Mock<IAuthenticatedClientCache>();
        var service =
            new MultifactorApiService(apiMock.Object, cacheMock.Object, NullLogger<MultifactorApiService>.Instance);

        await Assert.ThrowsAsync<ArgumentNullException>(() => service.CreateSecondFactorRequestAsync(null));
    }

    [Theory]
    [ClassData(typeof(EmptyStringsListInput))]
    public async Task CreateSecondFactorRequestAsync_NoIdentity_ShouldThrow(string identity)
    {
        var apiMock = new Mock<IMultifactorApi>();
        var cacheMock = new Mock<IAuthenticatedClientCache>();
        var service =
            new MultifactorApiService(apiMock.Object, cacheMock.Object, NullLogger<MultifactorApiService>.Instance);
        var contextMock = new Mock<IRadiusPipelineExecutionContext>();
        contextMock.Setup(x => x.LdapServerConfiguration.IdentityAttribute).Returns(identity);
        contextMock.Setup(x => x.LdapServerConfiguration.PhoneAttributes).Returns([]);
        contextMock.Setup(x => x.RequestPacket.UserName).Returns(identity);
        contextMock.Setup(x => x.ClientConfigurationName).Returns("configName");
        contextMock.Setup(x => x.AuthenticationCacheLifetime).Returns(AuthenticatedClientCacheConfig.Create("08:08:08", false));
        contextMock.Setup(x => x.ApiCredential).Returns(new ApiCredential("123", "123"));
        contextMock.Setup(x => x.LdapServerConfiguration.IdentityAttribute).Returns(string.Empty);
        contextMock.Setup(x => x.RequestPacket.UserName).Returns("username");
        contextMock.Setup(x => x.RequestPacket.TryGetChallenge()).Returns(() => null);
        contextMock.Setup(x => x.RequestPacket.CalledStationIdAttribute).Returns("CalledStationIdAttribute");
        contextMock.Setup(x => x.RequestPacket.CallingStationIdAttribute).Returns("CallingStationIdAttribute");
        contextMock.Setup(x => x.RemoteEndpoint).Returns(IPEndPoint.Parse("127.0.0.1:8080"));
        contextMock.Setup(x => x.UserLdapProfile.DisplayName).Returns("123");
        contextMock.Setup(x => x.UserLdapProfile.Email).Returns("email");
        contextMock.Setup(x => x.UserLdapProfile.Phone).Returns("phone");
        contextMock.Setup(x => x.UserLdapProfile.Attributes).Returns([new LdapAttribute("key", "value")]);
        contextMock.Setup(x => x.PrivacyMode).Returns(PrivacyModeDescriptor.Default);
        contextMock.Setup(x => x.PreAuthnMode).Returns(PreAuthModeDescriptor.Default);
        contextMock.Setup(x => x.UserNameTransformRules).Returns(new UserNameTransformRules());
        contextMock.Setup(x => x.BypassSecondFactorWhenApiUnreachable).Returns(true);
        contextMock.Setup(x => x.Passphrase).Returns(UserPassphrase.Parse("123456", PreAuthModeDescriptor.Default));
        
        var context = contextMock.Object;
        var response = await service.CreateSecondFactorRequestAsync(new CreateSecondFactorRequest(context));
        Assert.NotNull(response);
        Assert.Equal(AuthenticationStatus.Reject, response.Code);
    }

    [Theory]
    [ClassData(typeof(EmptyStringsListInput))]
    public async Task CreateSecondFactorRequestAsync_EmptyIdentityAttributeValue_ShouldThrow(string identity)
    {
        var apiMock = new Mock<IMultifactorApi>();
        var cacheMock = new Mock<IAuthenticatedClientCache>();
        var service =
            new MultifactorApiService(apiMock.Object, cacheMock.Object, NullLogger<MultifactorApiService>.Instance);
        var contextMock = new Mock<IRadiusPipelineExecutionContext>();
        contextMock.Setup(x => x.RequestPacket.UserName).Returns("username");
        contextMock.Setup(x => x.RequestPacket.CalledStationIdAttribute).Returns("CalledStationIdAttribute");
        contextMock.Setup(x => x.RequestPacket.CallingStationIdAttribute).Returns("CallingStationIdAttribute");
        contextMock.Setup(x => x.RemoteEndpoint).Returns(IPEndPoint.Parse("127.0.0.1:8080"));
        contextMock.Setup(x => x.UserLdapProfile.DisplayName).Returns("123");
        contextMock.Setup(x => x.UserLdapProfile.Email).Returns("email");
        contextMock.Setup(x => x.UserLdapProfile.Phone).Returns("phone");
        contextMock.Setup(x => x.ClientConfigurationName).Returns("config");
        contextMock.Setup(x => x.AuthenticationCacheLifetime).Returns(AuthenticatedClientCacheConfig.Create("08:08:08", false));
        contextMock.Setup(x => x.PrivacyMode).Returns(PrivacyModeDescriptor.Default);
        contextMock.Setup(x => x.Passphrase).Returns(UserPassphrase.Parse("123", PreAuthModeDescriptor.Default));
        contextMock.Setup(x => x.PreAuthnMode).Returns(PreAuthModeDescriptor.Default);
        contextMock.Setup(x => x.UserNameTransformRules).Returns(new UserNameTransformRules());
        contextMock.Setup(x => x.ApiCredential).Returns(new ApiCredential("123", "123"));
        contextMock.Setup(x => x.LdapServerConfiguration.IdentityAttribute).Returns("test");
        contextMock.Setup(x => x.UserLdapProfile.Attributes)
            .Returns([new LdapAttribute(new LdapAttributeName("test"), [identity])]);
        var context = contextMock.Object;
        var response = await service.CreateSecondFactorRequestAsync(new CreateSecondFactorRequest(context));
        Assert.NotNull(response);
        Assert.Equal(AuthenticationStatus.Reject, response.Code);
    }

    [Fact]
    public async Task CreateSecondFactorRequestAsync_BypassByCache_ShouldReturnBypass()
    {
        var apiMock = new Mock<IMultifactorApi>();
        var cacheMock = new Mock<IAuthenticatedClientCache>();
        cacheMock.Setup(x => x.TryHitCache(It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<AuthenticatedClientCacheConfig>())).Returns(true);
        var service = new MultifactorApiService(apiMock.Object, cacheMock.Object, NullLogger<MultifactorApiService>.Instance);

        var contextMock = new Mock<IRadiusPipelineExecutionContext>();
        
        contextMock.Setup(x => x.BypassSecondFactorWhenApiUnreachable).Returns(true);
        contextMock.Setup(x => x.LdapServerConfiguration.IdentityAttribute).Returns(string.Empty);
        contextMock.Setup(x => x.RequestPacket.UserName).Returns("username");
        contextMock.Setup(x => x.RequestPacket.CalledStationIdAttribute).Returns("CalledStationIdAttribute");
        contextMock.Setup(x => x.RequestPacket.CallingStationIdAttribute).Returns("CallingStationIdAttribute");
        contextMock.Setup(x => x.RemoteEndpoint).Returns(IPEndPoint.Parse("127.0.0.1:8080"));
        contextMock.Setup(x => x.UserLdapProfile.DisplayName).Returns("123");
        contextMock.Setup(x => x.UserLdapProfile.Email).Returns("email");
        contextMock.Setup(x => x.UserLdapProfile.Phone).Returns("phone");
        contextMock.Setup(x => x.UserLdapProfile.Attributes).Returns([]);
        contextMock.Setup(x => x.ClientConfigurationName).Returns("config");
        contextMock.Setup(x => x.AuthenticationCacheLifetime).Returns(AuthenticatedClientCacheConfig.Create("08:08:08", false));
        contextMock.Setup(x => x.PrivacyMode).Returns(PrivacyModeDescriptor.Default);
        contextMock.Setup(x => x.Passphrase).Returns(UserPassphrase.Parse("123", PreAuthModeDescriptor.Default));
        contextMock.Setup(x => x.PreAuthnMode).Returns(PreAuthModeDescriptor.Default);
        contextMock.Setup(x => x.UserNameTransformRules).Returns(new UserNameTransformRules());
        contextMock.Setup(x => x.ApiCredential).Returns(new ApiCredential("123", "123"));
        
        var context = contextMock.Object;
        var response = await service.CreateSecondFactorRequestAsync(new CreateSecondFactorRequest(context));
        Assert.NotNull(response);
        Assert.Equal(AuthenticationStatus.Bypass, response.Code);
    }

    [Theory]
    [InlineData(RequestStatus.AwaitingAuthentication, AuthenticationStatus.Awaiting, false)]
    [InlineData(RequestStatus.Granted, AuthenticationStatus.Accept, false)]
    [InlineData(RequestStatus.Granted, AuthenticationStatus.Bypass, true)]
    [InlineData(RequestStatus.Denied, AuthenticationStatus.Reject, false)]
    public async Task CreateSecondFactorRequestAsync_ShouldReturnStatus(RequestStatus status,
        AuthenticationStatus expectedStatus, bool bypass)
    {
        var apiMock = new Mock<IMultifactorApi>();
        apiMock
            .Setup(x => x.CreateAccessRequest(It.IsAny<AccessRequest>(), It.IsAny<ApiCredential>()))
            .ReturnsAsync(new AccessRequestResponse() { Status = status, Bypassed = bypass });

        var cacheMock = new Mock<IAuthenticatedClientCache>();
        cacheMock
            .Setup(x => x.TryHitCache(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<AuthenticatedClientCacheConfig>()))
            .Returns(false);
        var service =
            new MultifactorApiService(apiMock.Object, cacheMock.Object, NullLogger<MultifactorApiService>.Instance);

        var contextMock = new Mock<IRadiusPipelineExecutionContext>();
        contextMock.Setup(x => x.LdapServerConfiguration.IdentityAttribute).Returns(string.Empty);
        contextMock.Setup(x => x.RequestPacket.UserName).Returns("username");
        contextMock.Setup(x => x.RequestPacket.TryGetChallenge()).Returns(() => null);
        contextMock.Setup(x => x.RequestPacket.CalledStationIdAttribute).Returns("CalledStationIdAttribute");
        contextMock.Setup(x => x.RequestPacket.CallingStationIdAttribute).Returns("CallingStationIdAttribute");
        contextMock.Setup(x => x.RemoteEndpoint).Returns(IPEndPoint.Parse("127.0.0.1:8080"));
        contextMock.Setup(x => x.UserLdapProfile.DisplayName).Returns("123");
        contextMock.Setup(x => x.UserLdapProfile.Email).Returns("email");
        contextMock.Setup(x => x.UserLdapProfile.Phone).Returns("phone");
        contextMock.Setup(x => x.UserLdapProfile.Attributes).Returns([]);
        contextMock.Setup(x => x.PrivacyMode).Returns(PrivacyModeDescriptor.Default);
        contextMock.Setup(x => x.PreAuthnMode).Returns(PreAuthModeDescriptor.Default);
        contextMock.Setup(x => x.UserNameTransformRules).Returns(new UserNameTransformRules());
        contextMock.Setup(x => x.Passphrase).Returns(UserPassphrase.Parse("123456", PreAuthModeDescriptor.Default));
        contextMock.Setup(x => x.ClientConfigurationName).Returns("configName");
        contextMock.Setup(x => x.AuthenticationCacheLifetime).Returns(AuthenticatedClientCacheConfig.Create("08:08:08", false));
        contextMock.Setup(x => x.ApiCredential).Returns(new ApiCredential("123", "123"));
        
        var context = contextMock.Object;
        var response = await service.CreateSecondFactorRequestAsync(new CreateSecondFactorRequest(context));
        Assert.NotNull(response);
        Assert.Equal(expectedStatus, response.Code);
    }

    [Fact]
    public async Task CreateSecondFactorRequestAsync_MultifactorApiUnreachableExceptionNoBypass_ShouldReturnReject()
    {
        var apiMock = new Mock<IMultifactorApi>();
        apiMock
            .Setup(x => x.CreateAccessRequest(It.IsAny<AccessRequest>(), It.IsAny<ApiCredential>()))
            .ThrowsAsync(new MultifactorApiUnreachableException());

        var cacheMock = new Mock<IAuthenticatedClientCache>();
        cacheMock
            .Setup(x => x.TryHitCache(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<AuthenticatedClientCacheConfig>()))
            .Returns(false);
        var service =
            new MultifactorApiService(apiMock.Object, cacheMock.Object, NullLogger<MultifactorApiService>.Instance);

        var contextMock = new Mock<IRadiusPipelineExecutionContext>();
        
        contextMock.Setup(x => x.BypassSecondFactorWhenApiUnreachable).Returns(true);
        contextMock.Setup(x => x.Passphrase).Returns(UserPassphrase.Parse("123456", PreAuthModeDescriptor.Default));
        contextMock.Setup(x => x.ClientConfigurationName).Returns("configName");
        contextMock.Setup(x => x.AuthenticationCacheLifetime).Returns(AuthenticatedClientCacheConfig.Create("08:08:08", false));
        contextMock.Setup(x => x.ApiCredential).Returns(new ApiCredential("123", "123"));
        contextMock.Setup(x => x.LdapServerConfiguration.IdentityAttribute).Returns(string.Empty);
        contextMock.Setup(x => x.RequestPacket.UserName).Returns("username");
        contextMock.Setup(x => x.RequestPacket.TryGetChallenge()).Returns(() => null);
        contextMock.Setup(x => x.RequestPacket.CalledStationIdAttribute).Returns("CalledStationIdAttribute");
        contextMock.Setup(x => x.RequestPacket.CallingStationIdAttribute).Returns("CallingStationIdAttribute");
        contextMock.Setup(x => x.RemoteEndpoint).Returns(IPEndPoint.Parse("127.0.0.1:8080"));
        contextMock.Setup(x => x.UserLdapProfile.DisplayName).Returns("123");
        contextMock.Setup(x => x.UserLdapProfile.Email).Returns("email");
        contextMock.Setup(x => x.UserLdapProfile.Phone).Returns("phone");
        contextMock.Setup(x => x.UserLdapProfile.Attributes).Returns([]);
        contextMock.Setup(x => x.PrivacyMode).Returns(PrivacyModeDescriptor.Default);
        contextMock.Setup(x => x.PreAuthnMode).Returns(PreAuthModeDescriptor.Default);
        contextMock.Setup(x => x.UserNameTransformRules).Returns(new UserNameTransformRules());
        contextMock.Setup(x => x.BypassSecondFactorWhenApiUnreachable).Returns(false);
        contextMock.Setup(x => x.Passphrase).Returns(UserPassphrase.Parse("123456", PreAuthModeDescriptor.Default));

        var context = contextMock.Object;
        var response = await service.CreateSecondFactorRequestAsync(new CreateSecondFactorRequest(context));
        Assert.NotNull(response);
        Assert.Equal(AuthenticationStatus.Reject, response.Code);
    }

    [Fact]
    public async Task CreateSecondFactorRequestAsync_MultifactorApiUnreachableExceptionWithBypass_ShouldReturnBypass()
    {
        var apiMock = new Mock<IMultifactorApi>();
        apiMock
            .Setup(x => x.CreateAccessRequest(It.IsAny<AccessRequest>(), It.IsAny<ApiCredential>()))
            .ThrowsAsync(new MultifactorApiUnreachableException());

        var cacheMock = new Mock<IAuthenticatedClientCache>();
        cacheMock
            .Setup(x => x.TryHitCache(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<AuthenticatedClientCacheConfig>()))
            .Returns(false);
        var service =
            new MultifactorApiService(apiMock.Object, cacheMock.Object, NullLogger<MultifactorApiService>.Instance);

        var contextMock = new Mock<IRadiusPipelineExecutionContext>();
        contextMock.Setup(x => x.ClientConfigurationName).Returns("configName");
        contextMock.Setup(x => x.AuthenticationCacheLifetime).Returns(AuthenticatedClientCacheConfig.Create("08:08:08", false));
        contextMock.Setup(x => x.ApiCredential).Returns(new ApiCredential("123", "123"));
        contextMock.Setup(x => x.LdapServerConfiguration.IdentityAttribute).Returns(string.Empty);
        contextMock.Setup(x => x.RequestPacket.UserName).Returns("username");
        contextMock.Setup(x => x.RequestPacket.TryGetChallenge()).Returns(() => null);
        contextMock.Setup(x => x.RequestPacket.CalledStationIdAttribute).Returns("CalledStationIdAttribute");
        contextMock.Setup(x => x.RequestPacket.CallingStationIdAttribute).Returns("CallingStationIdAttribute");
        contextMock.Setup(x => x.RemoteEndpoint).Returns(IPEndPoint.Parse("127.0.0.1:8080"));
        contextMock.Setup(x => x.UserLdapProfile.DisplayName).Returns("123");
        contextMock.Setup(x => x.UserLdapProfile.Email).Returns("email");
        contextMock.Setup(x => x.UserLdapProfile.Phone).Returns("phone");
        contextMock.Setup(x => x.UserLdapProfile.Attributes).Returns([]);
        contextMock.Setup(x => x.PrivacyMode).Returns(PrivacyModeDescriptor.Default);
        contextMock.Setup(x => x.PreAuthnMode).Returns(PreAuthModeDescriptor.Default);
        contextMock.Setup(x => x.UserNameTransformRules).Returns(new UserNameTransformRules());
        contextMock.Setup(x => x.BypassSecondFactorWhenApiUnreachable).Returns(true);
        contextMock.Setup(x => x.Passphrase).Returns(UserPassphrase.Parse("123456", PreAuthModeDescriptor.Default));

        var context = contextMock.Object;
        var response = await service.CreateSecondFactorRequestAsync(new CreateSecondFactorRequest(context));
        Assert.NotNull(response);
        Assert.Equal(AuthenticationStatus.Bypass, response.Code);
    }

    [Fact]
    public async Task CreateSecondFactorRequestAsync_Expection_ShouldReturnReject()
    {
        var apiMock = new Mock<IMultifactorApi>();
        apiMock
            .Setup(x => x.CreateAccessRequest(It.IsAny<AccessRequest>(), It.IsAny<ApiCredential>()))
            .ThrowsAsync(new Exception());

        var cacheMock = new Mock<IAuthenticatedClientCache>();
        cacheMock
            .Setup(x => x.TryHitCache(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<AuthenticatedClientCacheConfig>()))
            .Returns(false);
        var service =
            new MultifactorApiService(apiMock.Object, cacheMock.Object, NullLogger<MultifactorApiService>.Instance);

        var contextMock = new Mock<IRadiusPipelineExecutionContext>();
        contextMock.Setup(x => x.LdapServerConfiguration.IdentityAttribute).Returns(string.Empty);
        contextMock.Setup(x => x.RequestPacket.UserName).Returns("username");
        contextMock.Setup(x => x.RequestPacket.TryGetChallenge()).Returns(() => null);
        contextMock.Setup(x => x.RequestPacket.CalledStationIdAttribute).Returns("CalledStationIdAttribute");
        contextMock.Setup(x => x.RequestPacket.CallingStationIdAttribute).Returns("CallingStationIdAttribute");
        contextMock.Setup(x => x.RemoteEndpoint).Returns(IPEndPoint.Parse("127.0.0.1:8080"));
        contextMock.Setup(x => x.UserLdapProfile.DisplayName).Returns("123");
        contextMock.Setup(x => x.UserLdapProfile.Email).Returns("email");
        contextMock.Setup(x => x.UserLdapProfile.Phone).Returns("phone");
        contextMock.Setup(x => x.UserLdapProfile.Attributes).Returns([]);
        contextMock.Setup(x => x.PrivacyMode).Returns(PrivacyModeDescriptor.Default);
        contextMock.Setup(x => x.PreAuthnMode).Returns(PreAuthModeDescriptor.Default);
        contextMock.Setup(x => x.UserNameTransformRules).Returns(new UserNameTransformRules());
        contextMock.Setup(x => x.BypassSecondFactorWhenApiUnreachable).Returns(true);
        contextMock.Setup(x => x.Passphrase).Returns(UserPassphrase.Parse("123456", PreAuthModeDescriptor.Default));
        contextMock.Setup(x => x.ClientConfigurationName).Returns("configName");
        contextMock.Setup(x => x.AuthenticationCacheLifetime).Returns(AuthenticatedClientCacheConfig.Create("08:08:08", false));
        contextMock.Setup(x => x.ApiCredential).Returns(new ApiCredential("123", "123"));
        var context = contextMock.Object;
        var response = await service.CreateSecondFactorRequestAsync(new CreateSecondFactorRequest(context));
        Assert.NotNull(response);
        Assert.Equal(AuthenticationStatus.Reject, response.Code);
    }

    [Fact]
    public async Task SendChallenge_NoContext_ShouldThrowException()
    {
        var apiMock = new Mock<IMultifactorApi>();
        var cacheMock = new Mock<IAuthenticatedClientCache>();
        var service = new MultifactorApiService(apiMock.Object, cacheMock.Object, NullLogger<MultifactorApiService>.Instance);

        await Assert.ThrowsAsync<ArgumentNullException>(() => service.SendChallengeAsync(null));
    }
    
    [Theory]
    [ClassData(typeof(EmptyStringsListInput))]
    public async Task SendChallenge_NoAnswer_ShouldThrowException(string answer)
    {
        var apiMock = new Mock<IMultifactorApi>();
        var cacheMock = new Mock<IAuthenticatedClientCache>();
        var service = new MultifactorApiService(apiMock.Object, cacheMock.Object, NullLogger<MultifactorApiService>.Instance);
        var context = new Mock<IRadiusPipelineExecutionContext>().Object;
        await Assert.ThrowsAnyAsync<ArgumentException>(() => service.SendChallengeAsync(new SendChallengeRequest(context, answer, "requestId")));
    }
    
    [Theory]
    [ClassData(typeof(EmptyStringsListInput))]
    public async Task SendChallenge_NoRequestId_ShouldThrowException(string requestId)
    {
        var apiMock = new Mock<IMultifactorApi>();
        var cacheMock = new Mock<IAuthenticatedClientCache>();
        var service = new MultifactorApiService(apiMock.Object, cacheMock.Object, NullLogger<MultifactorApiService>.Instance);
        var context = new Mock<IRadiusPipelineExecutionContext>().Object;
        await Assert.ThrowsAnyAsync<ArgumentException>(() => service.SendChallengeAsync(new SendChallengeRequest(context, "answer", requestId)));
    }
    
    [Theory]
    [ClassData(typeof(EmptyStringsListInput))]
    public async Task SendChallenge_NoIdentity_ShouldThrow(string identity)
    {
        var apiMock = new Mock<IMultifactorApi>();
        var cacheMock = new Mock<IAuthenticatedClientCache>();
        var service = new MultifactorApiService(apiMock.Object, cacheMock.Object, NullLogger<MultifactorApiService>.Instance);
        
        var contextMock = new Mock<IRadiusPipelineExecutionContext>();
        contextMock.Setup(x => x.AuthenticationCacheLifetime).Returns(AuthenticatedClientCacheConfig.Create("08:08:08", false));
        contextMock.Setup(x => x.UserLdapProfile).Returns(new Mock<ILdapProfile>().Object);
        contextMock.Setup(x => x.ClientConfigurationName).Returns("config");
        contextMock.Setup(x => x.ApiCredential).Returns(new ApiCredential("key", "secret"));
        contextMock.Setup(x => x.BypassSecondFactorWhenApiUnreachable).Returns(false);
        contextMock.Setup(x => x.RemoteEndpoint).Returns(IPEndPoint.Parse("127.0.0.1:8080"));
        
        contextMock.Setup(x => x.LdapServerConfiguration.IdentityAttribute).Returns(identity);
        contextMock.Setup(x => x.RequestPacket.UserName).Returns(identity);
        var context = contextMock.Object;
        await Assert.ThrowsAnyAsync<InvalidOperationException>(() => service.SendChallengeAsync(new SendChallengeRequest(context, "answer", "requestId")));
    }
    
    [Theory]
    [ClassData(typeof(EmptyStringsListInput))]
    public async Task SendChallenge_EmptyIdentityAttributeValue_ShouldThrow(string identity)
    {
        var apiMock = new Mock<IMultifactorApi>();
        var cacheMock = new Mock<IAuthenticatedClientCache>();
        var service = new MultifactorApiService(apiMock.Object, cacheMock.Object, NullLogger<MultifactorApiService>.Instance);
        
        var contextMock = new Mock<IRadiusPipelineExecutionContext>();
        contextMock.Setup(x => x.RequestPacket.UserName).Returns("username");
        contextMock.Setup(x => x.RequestPacket.CalledStationIdAttribute).Returns("CalledStationIdAttribute");
        contextMock.Setup(x => x.RequestPacket.CallingStationIdAttribute).Returns("CallingStationIdAttribute");
        contextMock.Setup(x => x.RemoteEndpoint).Returns(IPEndPoint.Parse("127.0.0.1:8080"));
        contextMock.Setup(x => x.UserLdapProfile.DisplayName).Returns("123");
        contextMock.Setup(x => x.UserLdapProfile.Email).Returns("email");
        contextMock.Setup(x => x.UserLdapProfile.Phone).Returns("phone");
        contextMock.Setup(x => x.ClientConfigurationName).Returns("config");
        contextMock.Setup(x => x.AuthenticationCacheLifetime).Returns(AuthenticatedClientCacheConfig.Create("08:08:08", false));
        contextMock.Setup(x => x.PrivacyMode).Returns(PrivacyModeDescriptor.Default);
        contextMock.Setup(x => x.Passphrase).Returns(UserPassphrase.Parse("123", PreAuthModeDescriptor.Default));
        contextMock.Setup(x => x.PreAuthnMode).Returns(PreAuthModeDescriptor.Default);
        contextMock.Setup(x => x.UserNameTransformRules).Returns(new UserNameTransformRules());
        contextMock.Setup(x => x.ApiCredential).Returns(new ApiCredential("123", "123"));
        contextMock.Setup(x => x.LdapServerConfiguration.IdentityAttribute).Returns("test");
        contextMock.Setup(x => x.UserLdapProfile.Attributes).Returns([new LdapAttribute(new LdapAttributeName("test"), [identity])]);
        
        var context = contextMock.Object;
        await Assert.ThrowsAnyAsync<InvalidOperationException>(() => service.SendChallengeAsync(new SendChallengeRequest(context, "answer", "requestId")));
    }

    [Theory]
    [InlineData(RequestStatus.Denied, AuthenticationStatus.Reject)]
    [InlineData(RequestStatus.Granted,  AuthenticationStatus.Accept)]
    [InlineData(RequestStatus.Granted,  AuthenticationStatus.Bypass, true)]
    [InlineData(RequestStatus.AwaitingAuthentication, AuthenticationStatus.Awaiting)]
    public async Task SendChallenge_ShouldReturnResponseCode(RequestStatus requestStatus, AuthenticationStatus expectedStatus, bool bypassed = false)
    {
        var apiMock = new Mock<IMultifactorApi>();
        apiMock
            .Setup(x => x.SendChallengeAsync(It.IsAny<ChallengeRequest>(), It.IsAny<ApiCredential>()))
            .ReturnsAsync(() => new AccessRequestResponse() { Status = requestStatus, Bypassed = bypassed});
        var cacheMock = new Mock<IAuthenticatedClientCache>();
        var service = new MultifactorApiService(apiMock.Object, cacheMock.Object, NullLogger<MultifactorApiService>.Instance);
        
        var contextMock = new Mock<IRadiusPipelineExecutionContext>();
        contextMock.Setup(x => x.RequestPacket.UserName).Returns("username");
        contextMock.Setup(x => x.RequestPacket.CalledStationIdAttribute).Returns("CalledStationIdAttribute");
        contextMock.Setup(x => x.RequestPacket.CallingStationIdAttribute).Returns("CallingStationIdAttribute");
        contextMock.Setup(x => x.RemoteEndpoint).Returns(IPEndPoint.Parse("127.0.0.1:8080"));
        contextMock.Setup(x => x.UserLdapProfile.DisplayName).Returns("123");
        contextMock.Setup(x => x.UserLdapProfile.Email).Returns("email");
        contextMock.Setup(x => x.UserLdapProfile.Phone).Returns("phone");
        contextMock.Setup(x => x.ClientConfigurationName).Returns("config");
        contextMock.Setup(x => x.AuthenticationCacheLifetime).Returns(AuthenticatedClientCacheConfig.Create("08:08:08", false));
        contextMock.Setup(x => x.PrivacyMode).Returns(PrivacyModeDescriptor.Default);
        contextMock.Setup(x => x.Passphrase).Returns(UserPassphrase.Parse("123", PreAuthModeDescriptor.Default));
        contextMock.Setup(x => x.PreAuthnMode).Returns(PreAuthModeDescriptor.Default);
        contextMock.Setup(x => x.UserNameTransformRules).Returns(new UserNameTransformRules());
        
        contextMock.Setup(x => x.LdapServerConfiguration.IdentityAttribute).Returns(string.Empty);
        contextMock.Setup(x => x.RequestPacket.UserName).Returns("username");
        contextMock.Setup(x => x.ApiCredential).Returns(new ApiCredential("key", "secret"));
        
        var response = await service.SendChallengeAsync(new SendChallengeRequest(contextMock.Object, "answer", "requestId"));
        Assert.NotNull(response);
        Assert.Equal(expectedStatus, response.Code);
    }

    [Fact]
    public async Task SendChallenge_MultifactorApiUnreachableExceptionNoBypass_ShouldReturnReject()
    {
        var apiMock = new Mock<IMultifactorApi>();
        apiMock
            .Setup(x => x.SendChallengeAsync(It.IsAny<ChallengeRequest>(), It.IsAny<ApiCredential>()))
            .ThrowsAsync(new MultifactorApiUnreachableException());
        var cacheMock = new Mock<IAuthenticatedClientCache>();
        var service = new MultifactorApiService(apiMock.Object, cacheMock.Object, NullLogger<MultifactorApiService>.Instance);
        
        var contextMock = new Mock<IRadiusPipelineExecutionContext>();
        contextMock.Setup(x => x.AuthenticationCacheLifetime).Returns(AuthenticatedClientCacheConfig.Create("08:08:08", false));
        contextMock.Setup(x => x.UserLdapProfile).Returns(new Mock<ILdapProfile>().Object);
        contextMock.Setup(x => x.ClientConfigurationName).Returns("config");
        contextMock.Setup(x => x.LdapServerConfiguration.IdentityAttribute).Returns(string.Empty);
        contextMock.Setup(x => x.RequestPacket.UserName).Returns("username");
        contextMock.Setup(x => x.ApiCredential).Returns(new ApiCredential("key", "secret"));
        contextMock.Setup(x => x.BypassSecondFactorWhenApiUnreachable).Returns(false);
        contextMock.Setup(x => x.RemoteEndpoint).Returns(IPEndPoint.Parse("127.0.0.1:8080"));
        
        var response = await service.SendChallengeAsync(new SendChallengeRequest(contextMock.Object, "answer", "requestId"));
        Assert.NotNull(response);
        Assert.Equal(AuthenticationStatus.Reject, response.Code);
    }
    
    [Fact]
    public async Task SendChallenge_MultifactorApiUnreachableExceptionBypass_ShouldReturnBypass()
    {
        var apiMock = new Mock<IMultifactorApi>();
        apiMock
            .Setup(x => x.SendChallengeAsync(It.IsAny<ChallengeRequest>(), It.IsAny<ApiCredential>()))
            .ThrowsAsync(new MultifactorApiUnreachableException());
        var cacheMock = new Mock<IAuthenticatedClientCache>();
        var service = new MultifactorApiService(apiMock.Object, cacheMock.Object, NullLogger<MultifactorApiService>.Instance);
        
        var contextMock = new Mock<IRadiusPipelineExecutionContext>();
        contextMock.Setup(x => x.AuthenticationCacheLifetime).Returns(AuthenticatedClientCacheConfig.Create("08:08:08", false));
        contextMock.Setup(x => x.UserLdapProfile).Returns(new Mock<ILdapProfile>().Object);
        contextMock.Setup(x => x.ClientConfigurationName).Returns("config");
        contextMock.Setup(x => x.LdapServerConfiguration.IdentityAttribute).Returns(string.Empty);
        contextMock.Setup(x => x.RequestPacket.UserName).Returns("username");
        contextMock.Setup(x => x.ApiCredential).Returns(new ApiCredential("key", "secret"));
        contextMock.Setup(x => x.BypassSecondFactorWhenApiUnreachable).Returns(true);
        contextMock.Setup(x => x.RemoteEndpoint).Returns(IPEndPoint.Parse("127.0.0.1:8080"));
        
        var response = await service.SendChallengeAsync(new SendChallengeRequest(contextMock.Object, "answer", "requestId"));
        Assert.NotNull(response);
        Assert.Equal(AuthenticationStatus.Bypass, response.Code);
    }
    
    [Fact]
    public async Task SendChallenge_Exception_ShouldReturnReject()
    {
        var apiMock = new Mock<IMultifactorApi>();
        apiMock
            .Setup(x => x.SendChallengeAsync(It.IsAny<ChallengeRequest>(), It.IsAny<ApiCredential>()))
            .ThrowsAsync(new Exception());
        var cacheMock = new Mock<IAuthenticatedClientCache>();
        var service = new MultifactorApiService(apiMock.Object, cacheMock.Object, NullLogger<MultifactorApiService>.Instance);
        
        var contextMock = new Mock<IRadiusPipelineExecutionContext>();
        contextMock.Setup(x => x.AuthenticationCacheLifetime).Returns(AuthenticatedClientCacheConfig.Create("08:08:08", false));
        contextMock.Setup(x => x.UserLdapProfile).Returns(new Mock<ILdapProfile>().Object);
        contextMock.Setup(x => x.ClientConfigurationName).Returns("config");
        contextMock.Setup(x => x.LdapServerConfiguration.IdentityAttribute).Returns(string.Empty);
        contextMock.Setup(x => x.RequestPacket.UserName).Returns("username");
        contextMock.Setup(x => x.ApiCredential).Returns(new ApiCredential("key", "secret"));
        contextMock.Setup(x => x.BypassSecondFactorWhenApiUnreachable).Returns(true);
        contextMock.Setup(x => x.RemoteEndpoint).Returns(IPEndPoint.Parse("127.0.0.1:8080"));
        
        var response = await service.SendChallengeAsync(new SendChallengeRequest(contextMock.Object, "answer", "requestId"));
        Assert.NotNull(response);
        Assert.Equal(AuthenticationStatus.Reject, response.Code);
    }
}