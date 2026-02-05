// using System.Net;
// using Microsoft.Extensions.Logging;
// using Moq;
// using Multifactor.Core.Ldap.Entry;
// using Multifactor.Radius.Adapter.v2.Application.Cache;
// using Multifactor.Radius.Adapter.v2.Application.Configuration.Models;
// using Multifactor.Radius.Adapter.v2.Application.Configuration.Models.Enum;
// using Multifactor.Radius.Adapter.v2.Application.Features.Ldap.Models;
// using Multifactor.Radius.Adapter.v2.Application.Features.Multifactor;
// using Multifactor.Radius.Adapter.v2.Application.Features.Multifactor.Exceptions;
// using Multifactor.Radius.Adapter.v2.Application.Features.Multifactor.Models;
// using Multifactor.Radius.Adapter.v2.Application.Features.Multifactor.Models.Enum;
// using Multifactor.Radius.Adapter.v2.Application.Features.Multifactor.Ports;
// using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.Models;
// using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.Models.Enum;
// using Multifactor.Radius.Adapter.v2.Application.Features.Radius.Models;
//
// namespace Multifactor.Radius.Adapter.v2.Tests.Application.Multifactor
// {
//     public class MultifactorApiServiceTests
//     {
//         private readonly Mock<IMultifactorApi> _apiMock;
//         private readonly Mock<IAuthenticatedClientCache> _cacheMock;
//         private readonly Mock<ILogger<MultifactorApiService>> _loggerMock;
//         private readonly MultifactorApiService _service;
//
//         public MultifactorApiServiceTests()
//         {
//             _apiMock = new Mock<IMultifactorApi>();
//             _cacheMock = new Mock<IAuthenticatedClientCache>();
//             _loggerMock = new Mock<ILogger<MultifactorApiService>>();
//             _service = new MultifactorApiService(_apiMock.Object, _cacheMock.Object, _loggerMock.Object);
//         }
//
//         [Fact]
//         public void Constructor_ShouldThrowArgumentNullException_WhenApiIsNull()
//         {
//             // Act & Assert
//             Assert.Throws<ArgumentNullException>(() => 
//                 new MultifactorApiService(null, _cacheMock.Object, _loggerMock.Object));
//         }
//
//         [Fact]
//         public void Constructor_ShouldThrowArgumentNullException_WhenCacheIsNull()
//         {
//             // Act & Assert
//             Assert.Throws<ArgumentNullException>(() => 
//                 new MultifactorApiService(_apiMock.Object, null, _loggerMock.Object));
//         }
//
//         [Fact]
//         public void Constructor_ShouldThrowArgumentNullException_WhenLoggerIsNull()
//         {
//             // Act & Assert
//             Assert.Throws<ArgumentNullException>(() => 
//                 new MultifactorApiService(_apiMock.Object, _cacheMock.Object, null));
//         }
//
//         [Fact]
//         public async Task CreateSecondFactorRequestAsync_ShouldThrowArgumentNullException_WhenContextIsNull()
//         {
//             // Act & Assert
//             await Assert.ThrowsAsync<ArgumentNullException>(() => _service.CreateSecondFactorRequestAsync(null, false));
//         }
//
//         [Fact]
//         public async Task CreateSecondFactorRequestAsync_ShouldReturnReject_WhenIdentityIsEmpty()
//         {
//             // Arrange
//
//             var header = new RadiusPacketHeader();
//             var requestPacket = new RadiusPacket(header)
//             {
//                 RemoteEndpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 1812)
//             };
//             var clientConfiguration = new ClientConfiguration();
//             var context = new RadiusPipelineContext(requestPacket, clientConfiguration) ;
//
//             // Act
//             var result = await _service.CreateSecondFactorRequestAsync(context, false);
//
//             // Assert
//             Assert.Equal(AuthenticationStatus.Reject, result.Code);
//         }
//
//         [Fact]
//         public async Task CreateSecondFactorRequestAsync_ShouldReturnBypass_WhenCacheHit()
//         {
//             // Arrange
//             var requestPacket = new RadiusPacket(It.IsAny<RadiusPacketHeader>())
//             {
//                 RemoteEndpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 1812),
//             };
//             requestPacket.AddAttributeValue("Calling-Station-Id", "192.168.1.1");
//             requestPacket.AddAttributeValue("User-Name", "testuser");
//             var clientConfiguration = new ClientConfiguration
//             {
//                 Name = "TestClient",
//                 AuthenticationCacheLifetime = TimeSpan.FromMinutes(30)
//             };
//             var context = new RadiusPipelineContext(requestPacket, clientConfiguration);
//
//             _cacheMock.Setup(x => x.TryHitCache(
//                 It.IsAny<string>(),
//                 "testuser",
//                 "TestClient",
//                 TimeSpan.FromMinutes(30)))
//                 .Returns(true);
//
//             // Act
//             var result = await _service.CreateSecondFactorRequestAsync(context, false);
//
//             // Assert
//             Assert.Equal(AuthenticationStatus.Bypass, result.Code);
//         }
//
//         [Fact]
//         public async Task CreateSecondFactorRequestAsync_ShouldCallApi_WhenCacheMiss()
//         {
//             // Arrange
//             var requestPacket = new RadiusPacket(It.IsAny<RadiusPacketHeader>())
//             {
//                 RemoteEndpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 1812)
//             };
//             requestPacket.AddAttributeValue("Calling-Station-Id", "192.168.1.1");
//             requestPacket.AddAttributeValue("User-Name", "testuser");
//             var clientConfiguration = new ClientConfiguration
//             {
//                 Name = "TestClient",
//                 MultifactorNasIdentifier = "nas-id",
//                 MultifactorSharedSecret = "shared-secret"
//             };
//             var context = new RadiusPipelineContext(requestPacket, clientConfiguration)
//             {
//                 LdapProfile = new LdapProfile(It.IsAny<LdapEntry>())
//                 {
//                     DisplayName = "Test User",
//                     Email = "test@example.com",
//                     Phone = "+1234567890"
//                 }
//             };
//
//             var expectedResponse = new AccessRequestResponse
//             {
//                 Status = RequestStatus.Granted,
//                 ReplyMessage = "Granted",
//                 Id = "request-id-123"
//             };
//
//             _cacheMock.Setup(x => x.TryHitCache(
//                 It.IsAny<string>(),
//                 It.IsAny<string>(),
//                 It.IsAny<string>(),
//                 It.IsAny<TimeSpan>()))
//                 .Returns(false);
//
//             _apiMock.Setup(x => x.CreateAccessRequest(
//                 It.IsAny<AccessRequestQuery>(),
//                 It.IsAny<MultifactorAuthData>(),
//                 It.IsAny<CancellationToken>()))
//                 .ReturnsAsync(expectedResponse);
//
//             // Act
//             var result = await _service.CreateSecondFactorRequestAsync(context, true);
//
//             // Assert
//             Assert.Equal(AuthenticationStatus.Accept, result.Code);
//             Assert.Equal("request-id-123", result.State);
//             Assert.Equal("Granted", result.ReplyMessage);
//             _apiMock.Verify(x => x.CreateAccessRequest(
//                 It.Is<AccessRequestQuery>(q => q.Identity == "testuser"),
//                 It.Is<MultifactorAuthData>(a => a.ApiSecret == "nas-id"),
//                 It.IsAny<CancellationToken>()), 
//                 Times.Once);
//         }
//
//         [Fact]
//         public async Task CreateSecondFactorRequestAsync_ShouldCacheResponse_WhenEnabledAndAccepted()
//         {
//             // Arrange
//             var header = new RadiusPacketHeader();
//
//             var requestPacket = new RadiusPacket(header)
//             {
//                 RemoteEndpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 1812),
//             };
//             requestPacket.AddAttributeValue("Calling-Station-Id", "192.168.1.1");
//             requestPacket.AddAttributeValue("User-Name", "testuser");
//             var clientConfiguration = new ClientConfiguration
//             {
//                 Name = "TestClient",
//                 MultifactorNasIdentifier = "nas-id",
//                 MultifactorSharedSecret = "shared-secret",
//                 AuthenticationCacheLifetime = TimeSpan.FromMinutes(30)
//             };
//             var context = new RadiusPipelineContext(requestPacket, clientConfiguration);
//
//             var apiResponse = new AccessRequestResponse
//             {
//                 Status = RequestStatus.Granted,
//                 Bypassed = false
//             };
//
//             _cacheMock.Setup(x => x.TryHitCache(
//                 It.IsAny<string>(),
//                 It.IsAny<string>(),
//                 It.IsAny<string>(),
//                 It.IsAny<TimeSpan>()))
//                 .Returns(false);
//
//             _apiMock.Setup(x => x.CreateAccessRequest(
//                 It.IsAny<AccessRequestQuery>(),
//                 It.IsAny<MultifactorAuthData>(),
//                 It.IsAny<CancellationToken>()))
//                 .ReturnsAsync(apiResponse);
//
//             // Act
//             var result = await _service.CreateSecondFactorRequestAsync(context, true);
//
//             // Assert
//             _cacheMock.Verify(x => x.SetCache(
//                 It.IsAny<string>(),
//                 "testuser",
//                 "TestClient",
//                 TimeSpan.FromMinutes(30)), 
//                 Times.Once);
//         }
//
//         [Fact]
//         public async Task CreateSecondFactorRequestAsync_ShouldNotCache_WhenBypassed()
//         {
//             // Arrange
//             var header = new RadiusPacketHeader();
//             var requestPacket = new RadiusPacket(header)
//             {
//                 RemoteEndpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 1812)
//             };
//             requestPacket.AddAttributeValue("User-Name", "testuser");
//             var clientConfiguration = new ClientConfiguration
//             {
//                 Name = "TestClient"
//             };
//             var context = new RadiusPipelineContext(requestPacket, clientConfiguration);
//
//             var apiResponse = new AccessRequestResponse
//             {
//                 Status = RequestStatus.Granted,
//                 Bypassed = true
//             };
//
//             _cacheMock.Setup(x => x.TryHitCache(
//                 It.IsAny<string>(),
//                 It.IsAny<string>(),
//                 It.IsAny<string>(),
//                 It.IsAny<TimeSpan>()))
//                 .Returns(false);
//
//             _apiMock.Setup(x => x.CreateAccessRequest(
//                 It.IsAny<AccessRequestQuery>(),
//                 It.IsAny<MultifactorAuthData>(),
//                 It.IsAny<CancellationToken>()))
//                 .ReturnsAsync(apiResponse);
//
//             // Act
//             var result = await _service.CreateSecondFactorRequestAsync(context, true);
//
//             // Assert
//             _cacheMock.Verify(x => x.SetCache(
//                 It.IsAny<string>(),
//                 It.IsAny<string>(),
//                 It.IsAny<string>(),
//                 It.IsAny<TimeSpan>()), 
//                 Times.Never);
//         }
//
//         [Fact]
//         public async Task CreateSecondFactorRequestAsync_ShouldApplyFullPrivacyMode()
//         {
//             // Arrange
//             var header = new RadiusPacketHeader();
//             var requestPacket = new RadiusPacket(header)
//             {
//                 UserName = "testuser",
//                 RemoteEndpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 1812),
//                 CallingStationIdAttribute = "192.168.1.1"
//             };
//             var clientConfiguration = new ClientConfiguration
//             {
//                 Name = "TestClient",
//                 MultifactorNasIdentifier = "nas-id",
//                 MultifactorSharedSecret = "shared-secret",
//                 Privacy = (PrivacyMode: PrivacyMode.Full, PrivacyFields: [])
//             };
//             var context = new RadiusPipelineContext(requestPacket, clientConfiguration)
//             {
//                 LdapProfile = new LdapProfile()
//                 {
//                     DisplayName = "Test User",
//                     Email = "test@example.com",
//                     Phone = "+1234567890"
//                 }
//             };
//
//             AccessRequestQuery capturedQuery = null;
//             _apiMock.Setup(x => x.CreateAccessRequest(
//                 It.IsAny<AccessRequestQuery>(),
//                 It.IsAny<MultifactorAuthData>(),
//                 It.IsAny<CancellationToken>()))
//                 .Callback<AccessRequestQuery, MultifactorAuthData>((q, a) => capturedQuery = q)
//                 .ReturnsAsync(new AccessRequestResponse { Status = RequestStatus.Granted });
//
//             // Act
//             await _service.CreateSecondFactorRequestAsync(context, false);
//
//             // Assert
//             Assert.NotNull(capturedQuery);
//             Assert.Null(capturedQuery.Name);
//             Assert.Null(capturedQuery.Email);
//             Assert.Null(capturedQuery.Phone);
//             Assert.Equal("", capturedQuery.CallingStationId);
//             Assert.Null(capturedQuery.CalledStationId);
//         }
//
//         [Fact]
//         public async Task CreateSecondFactorRequestAsync_ShouldReturnBypass_WhenApiUnreachableAndBypassEnabled()
//         {
//             // Arrange
//             var header = new RadiusPacketHeader();
//             var requestPacket = new RadiusPacket(header)
//             {
//                 UserName = "testuser",
//                 RemoteEndpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 1812)
//             };
//             var clientConfiguration = new ClientConfiguration
//             {
//                 Name = "TestClient",
//                 BypassSecondFactorWhenApiUnreachable = true
//             };
//             var ldapConfiguration = new LdapServerConfiguration()
//             {
//                 BypassSecondFactorWhenApiUnreachableGroups = new List<string> { "group1" }
//             };
//             var context = new RadiusPipelineContext(requestPacket, clientConfiguration, ldapConfiguration)
//             {
//                 
//                 UserGroups = ["group1"]
//             };
//
//             _apiMock.Setup(x => x.CreateAccessRequest(
//                 It.IsAny<AccessRequestQuery>(),
//                 It.IsAny<MultifactorAuthData>(),
//                 It.IsAny<CancellationToken>()))
//                 .ThrowsAsync(new MultifactorApiUnreachableException("API unreachable"));
//
//             // Act
//             var result = await _service.CreateSecondFactorRequestAsync(context, false);
//
//             // Assert
//             Assert.Equal(AuthenticationStatus.Bypass, result.Code);
//         }
//
//         [Fact]
//         public async Task CreateSecondFactorRequestAsync_ShouldReturnReject_WhenApiUnreachableAndBypassDisabled()
//         {
//             // Arrange
//             var header = new RadiusPacketHeader();
//             var requestPacket = new RadiusPacket(header)
//             {
//                 UserName = "testuser",
//                 RemoteEndpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 1812)
//             };
//             var clientConfiguration = new ClientConfiguration
//             {
//                 Name = "TestClient",
//                 BypassSecondFactorWhenApiUnreachable = false
//             };
//             var context = new RadiusPipelineContext(requestPacket, clientConfiguration);
//
//             _apiMock.Setup(x => x.CreateAccessRequest(
//                 It.IsAny<AccessRequestQuery>(),
//                 It.IsAny<MultifactorAuthData>(),
//                 It.IsAny<CancellationToken>()))
//                 .ThrowsAsync(new MultifactorApiUnreachableException("API unreachable"));
//
//             // Act
//             var result = await _service.CreateSecondFactorRequestAsync(context, false);
//
//             // Assert
//             Assert.Equal(AuthenticationStatus.Reject, result.Code);
//         }
//
//         [Fact]
//         public async Task SendChallengeAsync_ShouldThrowArgumentNullException_WhenContextIsNull()
//         {
//             // Act & Assert
//             await Assert.ThrowsAsync<ArgumentNullException>(() => 
//                 _service.SendChallengeAsync(null, false, "request-id", "answer"));
//         }
//
//         [Fact]
//         public async Task SendChallengeAsync_ShouldThrowArgumentException_WhenRequestIdIsNullOrEmpty()
//         {
//             // Arrange
//             var context = new RadiusPipelineContext(It.IsAny<RadiusPacket>(), It.IsAny<ClientConfiguration>());
//
//             // Act & Assert
//             await Assert.ThrowsAsync<ArgumentException>(() => 
//                 _service.SendChallengeAsync(context, false, "", "answer"));
//             
//             await Assert.ThrowsAsync<ArgumentException>(() => 
//                 _service.SendChallengeAsync(context, false, null, "answer"));
//         }
//
//         [Fact]
//         public async Task SendChallengeAsync_ShouldThrowArgumentException_WhenAnswerIsNullOrEmpty()
//         {
//             // Arrange
//             var context = new RadiusPipelineContext(It.IsAny<RadiusPacket>(), It.IsAny<ClientConfiguration>());
//
//             // Act & Assert
//             await Assert.ThrowsAsync<ArgumentException>(() => 
//                 _service.SendChallengeAsync(context, false, "request-id", ""));
//             
//             await Assert.ThrowsAsync<ArgumentException>(() => 
//                 _service.SendChallengeAsync(context, false, "request-id", null));
//         }
//
//         [Fact]
//         public async Task SendChallengeAsync_ShouldThrowInvalidOperationException_WhenIdentityIsEmpty()
//         {
//             // Arrange
//             var context = new RadiusPipelineContext
//             {
//                 RequestPacket = new RadiusPacket
//                 {
//                     UserName = ""
//                 }
//             };
//
//             // Act & Assert
//             await Assert.ThrowsAsync<InvalidOperationException>(() => 
//                 _service.SendChallengeAsync(context, false, "request-id", "answer"));
//         }
//
//         [Fact]
//         public async Task SendChallengeAsync_ShouldCallApiWithCorrectParameters()
//         {
//             // Arrange
//             var context = new RadiusPipelineContext
//             {
//                 RequestPacket = new RadiusPacket
//                 {
//                     UserName = "testuser",
//                     RemoteEndpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 1812),
//                     CallingStationIdAttribute = "192.168.1.1"
//                 },
//                 ClientConfiguration = new ClientConfiguration
//                 {
//                     Name = "TestClient",
//                     MultifactorNasIdentifier = "nas-id",
//                     MultifactorSharedSecret = "shared-secret",
//                     AuthenticationCacheLifetime = TimeSpan.FromMinutes(30)
//                 }
//             };
//
//             var expectedResponse = new AccessRequestResponse
//             {
//                 Status = RequestStatus.Granted,
//                 ReplyMessage = "Accepted"
//             };
//
//             _apiMock.Setup(x => x.SendChallengeAsync(
//                 It.IsAny<ChallengeRequestQuery>(),
//                 It.IsAny<MultifactorAuthData>(),
//                 It.IsAny<CancellationToken>()))
//                 .ReturnsAsync(expectedResponse);
//
//             // Act
//             var result = await _service.SendChallengeAsync(context, true, "request-123", "123456");
//
//             // Assert
//             _apiMock.Verify(x => x.SendChallengeAsync(
//                 It.Is<ChallengeRequestQuery>(q => 
//                     q.Identity == "testuser" && 
//                     q.Challenge == "123456" && 
//                     q.RequestId == "request-123"),
//                 It.Is<MultifactorAuthData>(a => 
//                     a.ApiKey == "nas-id" && 
//                     a.ApiSecret == "shared-secret")),
//                 Times.Once);
//         }
//
//         [Fact]
//         public async Task SendChallengeAsync_ShouldCacheResponse_WhenEnabledAndAccepted()
//         {
//             // Arrange
//             var requestPacket = new RadiusPacket
//             {
//                 UserName = "testuser",
//                 RemoteEndpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 1812),
//                 CallingStationIdAttribute = "192.168.1.1"
//             };
//             var clientConfiguration = new ClientConfiguration
//             {
//                 Name = "TestClient",
//                 MultifactorNasIdentifier = "nas-id",
//                 MultifactorSharedSecret = "shared-secret",
//                 AuthenticationCacheLifetime = TimeSpan.FromMinutes(30)
//             };
//             var context = new RadiusPipelineContext(requestPacket, clientConfiguration);
//
//             var apiResponse = new AccessRequestResponse
//             {
//                 Status = RequestStatus.Granted,
//                 Bypassed = false
//             };
//
//             _apiMock.Setup(x => x.SendChallengeAsync(
//                 It.IsAny<ChallengeRequestQuery>(),
//                 It.IsAny<MultifactorAuthData>(),
//                 It.IsAny<CancellationToken>()))
//                 .ReturnsAsync(apiResponse);
//
//             // Act
//             var result = await _service.SendChallengeAsync(context, true, "request-id", "answer");
//
//             // Assert
//             _cacheMock.Verify(x => x.SetCache(
//                 It.IsAny<string>(),
//                 "testuser",
//                 "TestClient",
//                 TimeSpan.FromMinutes(30)),
//                 Times.Once);
//         }
//
//         [Fact]
//         public void ConvertToAuthCode_ShouldReturnBypass_WhenGrantedAndBypassed()
//         {
//             // Arrange
//             var response = new AccessRequestResponse
//             {
//                 Status = RequestStatus.Granted,
//                 Bypassed = true
//             };
//
//             // Act
//             var result = InvokePrivateMethod<AuthenticationStatus>("ConvertToAuthCode", response);
//
//             // Assert
//             Assert.Equal(AuthenticationStatus.Bypass, result);
//         }
//
//         [Fact]
//         public void ConvertToAuthCode_ShouldReturnAccept_WhenGrantedAndNotBypassed()
//         {
//             // Arrange
//             var response = new AccessRequestResponse
//             {
//                 Status = RequestStatus.Granted,
//                 Bypassed = false
//             };
//
//             // Act
//             var result = InvokePrivateMethod<AuthenticationStatus>("ConvertToAuthCode", response);
//
//             // Assert
//             Assert.Equal(AuthenticationStatus.Accept, result);
//         }
//
//         [Fact]
//         public void ConvertToAuthCode_ShouldReturnReject_WhenDenied()
//         {
//             // Arrange
//             var response = new AccessRequestResponse
//             {
//                 Status = RequestStatus.Denied
//             };
//
//             // Act
//             var result = InvokePrivateMethod<AuthenticationStatus>("ConvertToAuthCode", response);
//
//             // Assert
//             Assert.Equal(AuthenticationStatus.Reject, result);
//         }
//
//         [Fact]
//         public void ConvertToAuthCode_ShouldReturnAwaiting_WhenAwaitingAuthentication()
//         {
//             // Arrange
//             var response = new AccessRequestResponse
//             {
//                 Status = RequestStatus.AwaitingAuthentication
//             };
//
//             // Act
//             var result = InvokePrivateMethod<AuthenticationStatus>("ConvertToAuthCode", response);
//
//             // Assert
//             Assert.Equal(AuthenticationStatus.Awaiting, result);
//         }
//
//         [Fact]
//         public void ConvertToAuthCode_ShouldReturnReject_WhenResponseIsNull()
//         {
//             // Act
//             var result = InvokePrivateMethod<AuthenticationStatus>("ConvertToAuthCode", (AccessRequestResponse)null);
//
//             // Assert
//             Assert.Equal(AuthenticationStatus.Reject, result);
//         }
//
//         private static T InvokePrivateMethod<T>(string methodName, params object[] parameters)
//         {
//             var method = typeof(MultifactorApiService).GetMethod(
//                 methodName,
//                 System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
//             
//             if (method == null)
//                 throw new ArgumentException($"Method {methodName} not found");
//             
//             return (T)method.Invoke(new MultifactorApiService(
//                 Mock.Of<IMultifactorApi>(),
//                 Mock.Of<IAuthenticatedClientCache>(),
//                 Mock.Of<ILogger<MultifactorApiService>>()), parameters);
//         }
//     }
// }