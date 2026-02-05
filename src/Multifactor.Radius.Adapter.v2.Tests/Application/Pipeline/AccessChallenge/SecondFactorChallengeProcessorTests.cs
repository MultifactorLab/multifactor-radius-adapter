// using System.Text;
// using Microsoft.Extensions.Caching.Memory;
// using Microsoft.Extensions.Logging;
// using Moq;
// using Multifactor.Radius.Adapter.v2.Application.Configuration.Models;
// using Multifactor.Radius.Adapter.v2.Application.Features.Ldap.Ports;
// using Multifactor.Radius.Adapter.v2.Application.Features.Multifactor;
// using Multifactor.Radius.Adapter.v2.Application.Features.Multifactor.Models;
// using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.AccessChallenge;
// using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.AccessChallenge.Models;
// using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.AccessChallenge.Models.Enums;
// using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.Models;
// using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.Models.Enum;
// using Multifactor.Radius.Adapter.v2.Application.Features.Radius.Models;
// using Multifactor.Radius.Adapter.v2.Application.Features.Radius.Models.Enums;
//
// namespace Multifactor.Radius.Adapter.v2.Tests.Application.Pipeline.AccessChallenge
// {
//     public class SecondFactorChallengeProcessorTests
//     {
//         private readonly Mock<IMemoryCache> _memoryCacheMock;
//         private readonly Mock<MultifactorApiService> _apiServiceMock;
//         private readonly Mock<ILdapAdapter> _ldapAdapterMock;
//         private readonly Mock<ILogger<SecondFactorChallengeProcessor>> _loggerMock;
//         private readonly SecondFactorChallengeProcessor _processor;
//
//         public SecondFactorChallengeProcessorTests()
//         {
//             _memoryCacheMock = new Mock<IMemoryCache>();
//             _apiServiceMock = new Mock<MultifactorApiService>();
//             _ldapAdapterMock = new Mock<ILdapAdapter>();
//             _loggerMock = new Mock<ILogger<SecondFactorChallengeProcessor>>();
//             _processor = new SecondFactorChallengeProcessor(
//                 _apiServiceMock.Object,
//                 _ldapAdapterMock.Object,
//                 _loggerMock.Object,
//                 _memoryCacheMock.Object);
//         }
//
//         [Fact]
//         public void ChallengeType_ShouldReturnSecondFactor()
//         {
//             // Assert
//             Assert.Equal(ChallengeType.SecondFactor, _processor.ChallengeType);
//         }
//
//         [Fact]
//         public void Constructor_ShouldThrowArgumentNullException_WhenMemoryCacheIsNull()
//         {
//             // Act & Assert
//             Assert.Throws<ArgumentNullException>(() => 
//                 new SecondFactorChallengeProcessor(
//                     _apiServiceMock.Object,
//                     _ldapAdapterMock.Object,
//                     _loggerMock.Object,
//                     null));
//         }
//
//         [Fact]
//         public void AddChallengeContext_ShouldThrowArgumentNullException_WhenContextIsNull()
//         {
//             // Act & Assert
//             Assert.Throws<ArgumentNullException>(() => _processor.AddChallengeContext(null));
//         }
//
//         [Fact]
//         public void AddChallengeContext_ShouldThrowArgumentException_WhenStateIsNullOrWhiteSpace()
//         {
//             // Arrange
//             var context = It.IsAny<RadiusPipelineContext>();
//
//             // Act & Assert
//             Assert.Throws<ArgumentException>(() => _processor.AddChallengeContext(context));
//         }
//
//         [Fact]
//         public void AddChallengeContext_ShouldAddContextToCacheAndReturnIdentifier()
//         {
//             // Arrange
//             var state = "test-state-123";
//             var context = new RadiusPipelineContext
//             {
//                 ClientConfiguration = new ClientConfiguration { Name = "TestClient" },
//                 ResponseInformation = new ResponseInformation { State = state },
//                 RequestPacket = new RadiusPacket { Identifier = 123 }
//             };
//
//             object cacheEntry = null;
//             var cacheKey = $"Challenge:TestClient:{state}";
//             
//             _memoryCacheMock.Setup(x => x.Set(
//                 It.IsAny<string>(),
//                 It.IsAny<object>(),
//                 It.IsAny<MemoryCacheEntryOptions>()))
//                 .Callback<string, object, MemoryCacheEntryOptions>((key, value, options) =>
//                 {
//                     cacheEntry = value;
//                 });
//
//             // Act
//             var identifier = _processor.AddChallengeContext(context);
//
//             // Assert
//             Assert.Equal("TestClient", identifier.ClientId);
//             Assert.Equal(state, identifier.RequestId);
//             _memoryCacheMock.Verify(x => x.Set(
//                 It.Is<string>(k => k == cacheKey),
//                 It.Is<RadiusPipelineContext>(c => c == context),
//                 It.IsAny<MemoryCacheEntryOptions>()), Times.Once);
//         }
//
//         [Fact]
//         public void AddChallengeContext_ShouldReturnEmptyIdentifier_WhenCacheFails()
//         {
//             // Arrange
//             var context = new RadiusPipelineContext
//             {
//                 ClientConfiguration = new ClientConfiguration { Name = "TestClient" },
//                 ResponseInformation = new ResponseInformation { State = "state" },
//                 RequestPacket = new RadiusPacket { Identifier = 123 }
//             };
//
//             _memoryCacheMock.Setup(x => x.Set(
//                 It.IsAny<string>(),
//                 It.IsAny<object>(),
//                 It.IsAny<MemoryCacheEntryOptions>()))
//                 .Throws(new Exception("Cache error"));
//
//             // Act
//             var identifier = _processor.AddChallengeContext(context);
//
//             // Assert
//             Assert.Equal(ChallengeIdentifier.Empty, identifier);
//         }
//
//         [Fact]
//         public void HasChallengeContext_ShouldReturnTrue_WhenContextExistsInCache()
//         {
//             // Arrange
//             var identifier = new ChallengeIdentifier("TestClient", "state-123");
//             var cacheKey = $"Challenge:TestClient:state-123";
//             
//             object cachedValue = new object();
//             _memoryCacheMock.Setup(x => x.TryGetValue(cacheKey, out It.Ref<object>.IsAny))
//                 .Callback(new TryGetValueCallback((string key, out object value) => value = cachedValue))
//                 .Returns(true);
//
//             // Act
//             var result = _processor.HasChallengeContext(identifier);
//
//             // Assert
//             Assert.True(result);
//         }
//
//         [Fact]
//         public void HasChallengeContext_ShouldReturnFalse_WhenContextNotInCache()
//         {
//             // Arrange
//             var identifier = new ChallengeIdentifier("TestClient", "state-123");
//             var cacheKey = $"Challenge:TestClient:state-123";
//             
//             object cachedValue = null;
//             _memoryCacheMock.Setup(x => x.TryGetValue(cacheKey, out It.Ref<object>.IsAny))
//                 .Callback(new TryGetValueCallback((string key, out object value) => value = null))
//                 .Returns(false);
//
//             // Act
//             var result = _processor.HasChallengeContext(identifier);
//
//             // Assert
//             Assert.False(result);
//         }
//
//         [Fact]
//         public async Task ProcessChallengeAsync_ShouldReturnReject_WhenUserNameIsEmpty()
//         {
//             // Arrange
//             var identifier = new ChallengeIdentifier("client", "state");
//             var context = new RadiusPipelineContext
//             {
//                 RequestPacket = new RadiusPacket
//                 {
//                     UserName = "",
//                     Identifier = 1,
//                     RemoteEndpoint = new System.Net.IPEndPoint(System.Net.IPAddress.Parse("127.0.0.1"), 1812)
//                 },
//                 ResponseInformation = new ResponseInformation()
//             };
//
//             // Act
//             var result = await _processor.ProcessChallengeAsync(identifier, context);
//
//             // Assert
//             Assert.Equal(ChallengeStatus.Reject, result);
//             Assert.Equal(AuthenticationStatus.Reject, context.SecondFactorStatus);
//             Assert.Equal("state", context.ResponseInformation.State);
//         }
//
//         [Fact]
//         public async Task ProcessChallengeAsync_ShouldReturnReject_WhenPAPAuthenticationWithEmptyPassword()
//         {
//             // Arrange
//             var identifier = new ChallengeIdentifier("client", "state");
//             var context = new RadiusPipelineContext
//             {
//                 RequestPacket = new RadiusPacket
//                 {
//                     UserName = "testuser",
//                     Identifier = 1,
//                     AuthenticationType = AuthenticationType.PAP,
//                     RemoteEndpoint = new System.Net.IPEndPoint(System.Net.IPAddress.Parse("127.0.0.1"), 1812)
//                 },
//                 Passphrase = new UserPassphrase { Raw = "" },
//                 ResponseInformation = new ResponseInformation()
//             };
//
//             // Act
//             var result = await _processor.ProcessChallengeAsync(identifier, context);
//
//             // Assert
//             Assert.Equal(ChallengeStatus.Reject, result);
//             Assert.Equal(AuthenticationStatus.Reject, context.SecondFactorStatus);
//             Assert.Equal("state", context.ResponseInformation.State);
//         }
//
//         [Fact]
//         public async Task ProcessChallengeAsync_ShouldReturnReject_WhenMSCHAP2WithoutResponse()
//         {
//             // Arrange
//             var identifier = new ChallengeIdentifier("client", "state");
//             var context = new RadiusPipelineContext
//             {
//                 RequestPacket = new RadiusPacket
//                 {
//                     UserName = "testuser",
//                     Identifier = 1,
//                     AuthenticationType = AuthenticationType.MSCHAP2,
//                     RemoteEndpoint = new System.Net.IPEndPoint(System.Net.IPAddress.Parse("127.0.0.1"), 1812)
//                 },
//                 ResponseInformation = new ResponseInformation()
//             };
//
//             // Act
//             var result = await _processor.ProcessChallengeAsync(identifier, context);
//
//             // Assert
//             Assert.Equal(ChallengeStatus.Reject, result);
//             Assert.Equal(AuthenticationStatus.Reject, context.SecondFactorStatus);
//             Assert.Equal("state", context.ResponseInformation.State);
//         }
//
//         [Fact]
//         public async Task ProcessChallengeAsync_ShouldReturnReject_WhenUnsupportedAuthenticationType()
//         {
//             // Arrange
//             var identifier = new ChallengeIdentifier("client", "state");
//             var context = new RadiusPipelineContext
//             {
//                 RequestPacket = new RadiusPacket
//                 {
//                     UserName = "testuser",
//                     Identifier = 1,
//                     AuthenticationType = (AuthenticationType)999, // Invalid type
//                     RemoteEndpoint = new System.Net.IPEndPoint(System.Net.IPAddress.Parse("127.0.0.1"), 1812)
//                 },
//                 ResponseInformation = new ResponseInformation()
//             };
//
//             // Act
//             var result = await _processor.ProcessChallengeAsync(identifier, context);
//
//             // Assert
//             Assert.Equal(ChallengeStatus.Reject, result);
//             Assert.Equal(AuthenticationStatus.Reject, context.SecondFactorStatus);
//             Assert.Equal("state", context.ResponseInformation.State);
//         }
//
//         [Fact]
//         public async Task ProcessChallengeAsync_ShouldThrowInvalidOperationException_WhenContextNotFound()
//         {
//             // Arrange
//             var identifier = new ChallengeIdentifier("client", "state");
//             var context = new RadiusPipelineContext
//             {
//                 RequestPacket = new RadiusPacket
//                 {
//                     UserName = "testuser",
//                     Identifier = 1,
//                     AuthenticationType = AuthenticationType.PAP,
//                     RemoteEndpoint = new System.Net.IPEndPoint(System.Net.IPAddress.Parse("127.0.0.1"), 1812)
//                 },
//                 Passphrase = new UserPassphrase { Raw = "password123" },
//                 ResponseInformation = new ResponseInformation()
//             };
//
//             var cacheKey = $"Challenge:client:state";
//             object cachedValue = null;
//             _memoryCacheMock.Setup(x => x.TryGetValue(cacheKey, out It.Ref<object>.IsAny))
//                 .Callback(new TryGetValueCallback((string key, out object value) => value = null))
//                 .Returns(false);
//
//             // Act & Assert
//             await Assert.ThrowsAsync<InvalidOperationException>(() => 
//                 _processor.ProcessChallengeAsync(identifier, context));
//         }
//
//         [Fact]
//         public async Task ProcessChallengeAsync_ShouldProcessPAPAuthentication()
//         {
//             // Arrange
//             var identifier = new ChallengeIdentifier("client", "state");
//             var challengeContext = new RadiusPipelineContext();
//             var context = new RadiusPipelineContext
//             {
//                 RequestPacket = new RadiusPacket
//                 {
//                     UserName = "testuser",
//                     Identifier = 1,
//                     AuthenticationType = AuthenticationType.PAP,
//                     RemoteEndpoint = new System.Net.IPEndPoint(System.Net.IPAddress.Parse("127.0.0.1"), 1812)
//                 },
//                 Passphrase = new UserPassphrase { Raw = "password123" },
//                 ResponseInformation = new ResponseInformation()
//             };
//
//             var cacheKey = $"Challenge:client:state";
//             _memoryCacheMock.Setup(x => x.TryGetValue(cacheKey, out It.Ref<object>.IsAny))
//                 .Callback(new TryGetValueCallback((string key, out object value) => value = challengeContext))
//                 .Returns(true);
//
//             var apiResponse = new SecondFactorResponse
//             {
//                 Code = AuthenticationStatus.Accept,
//                 ReplyMessage = "Accepted"
//             };
//
//             _apiServiceMock.Setup(x => x.SendChallengeAsync(
//                 challengeContext,
//                 It.IsAny<bool>(),
//                 identifier.RequestId,
//                 "password123"))
//                 .ReturnsAsync(apiResponse);
//
//             // Act
//             var result = await _processor.ProcessChallengeAsync(identifier, context);
//
//             // Assert
//             Assert.Equal(ChallengeStatus.Accept, result);
//             Assert.Equal("Accepted", context.ResponseInformation.ReplyMessage);
//             Assert.Equal(AuthenticationStatus.Accept, context.SecondFactorStatus);
//             _memoryCacheMock.Verify(x => x.Remove(cacheKey), Times.Once);
//         }
//
//         [Fact]
//         public async Task ProcessChallengeAsync_ShouldProcessMSCHAP2Authentication()
//         {
//             // Arrange
//             var identifier = new ChallengeIdentifier("client", "state");
//             var challengeContext = new RadiusPipelineContext();
//             var context = new RadiusPipelineContext
//             {
//                 RequestPacket = new RadiusPacket
//                 {
//                     UserName = "testuser",
//                     Identifier = 1,
//                     AuthenticationType = AuthenticationType.MSCHAP2,
//                     RemoteEndpoint = new System.Net.IPEndPoint(System.Net.IPAddress.Parse("127.0.0.1"), 1812)
//                 },
//                 ResponseInformation = new ResponseInformation()
//             };
//
//             var otpBytes = Encoding.ASCII.GetBytes("123456");
//             var msChapResponse = new byte[] { 0x00, 0x00 }.Concat(otpBytes).ToArray();
//             
//             var mockRequestPacket = new Mock<RadiusPacket>();
//             mockRequestPacket.Setup(x => x.GetAttribute<byte[]?>("MS-CHAP2-Response"))
//                 .Returns(msChapResponse);
//             mockRequestPacket.Setup(x => x.UserName).Returns("testuser");
//             mockRequestPacket.Setup(x => x.Identifier).Returns(1);
//             mockRequestPacket.Setup(x => x.AuthenticationType).Returns(AuthenticationType.MSCHAP2);
//             mockRequestPacket.Setup(x => x.RemoteEndpoint).Returns(new System.Net.IPEndPoint(System.Net.IPAddress.Parse("127.0.0.1"), 1812));
//             
//             context.RequestPacket = mockRequestPacket.Object;
//
//             var cacheKey = $"Challenge:client:state";
//             _memoryCacheMock.Setup(x => x.TryGetValue(cacheKey, out It.Ref<object>.IsAny))
//                 .Callback(new TryGetValueCallback((string key, out object value) => value = challengeContext))
//                 .Returns(true);
//
//             var apiResponse = new SecondFactorResponse
//             {
//                 Code = AuthenticationStatus.Accept,
//                 ReplyMessage = "Accepted"
//             };
//
//             _apiServiceMock.Setup(x => x.SendChallengeAsync(
//                 challengeContext,
//                 It.IsAny<bool>(),
//                 identifier.RequestId,
//                 "123456"))
//                 .ReturnsAsync(apiResponse);
//
//             // Act
//             var result = await _processor.ProcessChallengeAsync(identifier, context);
//
//             // Assert
//             Assert.Equal(ChallengeStatus.Accept, result);
//             Assert.Equal("Accepted", context.ResponseInformation.ReplyMessage);
//             Assert.Equal(AuthenticationStatus.Accept, context.SecondFactorStatus);
//             _memoryCacheMock.Verify(x => x.Remove(cacheKey), Times.Once);
//         }
//
//         [Fact]
//         public async Task ProcessChallengeAsync_ShouldReturnInProcess_WhenApiReturnsOtherStatus()
//         {
//             // Arrange
//             var identifier = new ChallengeIdentifier("client", "state");
//             var challengeContext = new RadiusPipelineContext();
//             var context = new RadiusPipelineContext
//             {
//                 RequestPacket = new RadiusPacket
//                 {
//                     UserName = "testuser",
//                     Identifier = 1,
//                     AuthenticationType = AuthenticationType.PAP,
//                     RemoteEndpoint = new System.Net.IPEndPoint(System.Net.IPAddress.Parse("127.0.0.1"), 1812)
//                 },
//                 Passphrase = new UserPassphrase { Raw = "password123" },
//                 ResponseInformation = new ResponseInformation()
//             };
//
//             var cacheKey = $"Challenge:client:state";
//             _memoryCacheMock.Setup(x => x.TryGetValue(cacheKey, out It.Ref<object>.IsAny))
//                 .Callback(new TryGetValueCallback((string key, out object value) => value = challengeContext))
//                 .Returns(true);
//
//             var apiResponse = new SecondFactorResponse
//             {
//                 Code = AuthenticationStatus.Challenge,
//                 ReplyMessage = "Continue"
//             };
//
//             _apiServiceMock.Setup(x => x.SendChallengeAsync(
//                 challengeContext,
//                 It.IsAny<bool>(),
//                 identifier.RequestId,
//                 "password123"))
//                 .ReturnsAsync(apiResponse);
//
//             // Act
//             var result = await _processor.ProcessChallengeAsync(identifier, context);
//
//             // Assert
//             Assert.Equal(ChallengeStatus.InProcess, result);
//             Assert.Equal("Continue", context.ResponseInformation.ReplyMessage);
//             Assert.Equal("state", context.ResponseInformation.State);
//         }
//
//         [Fact]
//         public async Task ProcessChallengeAsync_ShouldReturnReject_WhenApiReturnsReject()
//         {
//             // Arrange
//             var identifier = new ChallengeIdentifier("client", "state");
//             var challengeContext = new RadiusPipelineContext();
//             var context = new RadiusPipelineContext
//             {
//                 RequestPacket = new RadiusPacket
//                 {
//                     UserName = "testuser",
//                     Identifier = 1,
//                     AuthenticationType = AuthenticationType.PAP,
//                     RemoteEndpoint = new System.Net.IPEndPoint(System.Net.IPAddress.Parse("127.0.0.1"), 1812)
//                 },
//                 Passphrase = new UserPassphrase { Raw = "password123" },
//                 ResponseInformation = new ResponseInformation()
//             };
//
//             var cacheKey = $"Challenge:client:state";
//             _memoryCacheMock.Setup(x => x.TryGetValue(cacheKey, out It.Ref<object>.IsAny))
//                 .Callback(new TryGetValueCallback((string key, out object value) => value = challengeContext))
//                 .Returns(true);
//
//             var apiResponse = new SecondFactorResponse
//             {
//                 Code = AuthenticationStatus.Reject,
//                 ReplyMessage = "Rejected"
//             };
//
//             _apiServiceMock.Setup(x => x.SendChallengeAsync(
//                 challengeContext,
//                 It.IsAny<bool>(),
//                 identifier.RequestId,
//                 "password123"))
//                 .ReturnsAsync(apiResponse);
//
//             // Act
//             var result = await _processor.ProcessChallengeAsync(identifier, context);
//
//             // Assert
//             Assert.Equal(ChallengeStatus.Reject, result);
//             Assert.Equal("Rejected", context.ResponseInformation.ReplyMessage);
//             Assert.Equal(AuthenticationStatus.Reject, context.SecondFactorStatus);
//             Assert.Equal("state", context.ResponseInformation.State);
//             _memoryCacheMock.Verify(x => x.Remove(cacheKey), Times.Once);
//         }
//
//         [Fact]
//         public void ShouldCacheResponse_ShouldReturnTrue_WhenNoLdapConfiguration()
//         {
//             // Arrange
//             var context = new RadiusPipelineContext
//             {
//                 LdapConfiguration = null,
//                 RequestPacket = new RadiusPacket { UserName = "testuser" }
//             };
//
//             // Act
//             var result = _processor.GetType().GetMethod("ShouldCacheResponse", 
//                 System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
//                 .Invoke(_processor, new object[] { context });
//
//             // Assert
//             Assert.True((bool)result);
//         }
//
//         [Fact]
//         public void ShouldCacheResponse_ShouldReturnTrue_WhenNoAuthenticationCacheGroups()
//         {
//             // Arrange
//             var context = new  RadiusPipelineContext
//             {
//                 LdapConfiguration = new LdapServerConfiguration()
//                 {
//                     AuthenticationCacheGroups = new List<string>()
//                 },
//                 RequestPacket = new RadiusPacket() { UserName = "testuser" }
//             };
//
//             // Act
//             var result = _processor.GetType().GetMethod("ShouldCacheResponse", 
//                 System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
//                 .Invoke(_processor, new object[] { context });
//
//             // Assert
//             Assert.True((bool)result);
//         }
//     }
// }