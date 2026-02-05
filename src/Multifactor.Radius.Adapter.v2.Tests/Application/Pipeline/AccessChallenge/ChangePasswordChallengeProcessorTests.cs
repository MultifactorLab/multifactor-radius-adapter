// using Microsoft.Extensions.Logging;
// using Moq;
// using Multifactor.Core.Ldap.Schema;
// using Multifactor.Radius.Adapter.v2.Application.Cache;
// using Multifactor.Radius.Adapter.v2.Application.Configuration.Models;
// using Multifactor.Radius.Adapter.v2.Application.Configuration.Models.Enum;
// using Multifactor.Radius.Adapter.v2.Application.Features.Ldap.Models;
// using Multifactor.Radius.Adapter.v2.Application.Features.Ldap.Ports;
// using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.AccessChallenge;
// using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.AccessChallenge.Models;
// using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.AccessChallenge.Models.Enums;
// using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.Models;
// using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.Models.Enum;
// using Multifactor.Radius.Adapter.v2.Application.Features.Radius.Models;
// using Multifactor.Radius.Adapter.v2.Application.Features.Security;
//
// namespace Multifactor.Radius.Adapter.v2.Tests.Application.Pipeline.AccessChallenge
// {
//     public class ChangePasswordChallengeProcessorTests
//     {
//         private readonly Mock<ICacheService> _cacheMock;
//         private readonly Mock<ILdapAdapter> _ldapAdapterMock;
//         private readonly Mock<ILogger<ChangePasswordChallengeProcessor>> _loggerMock;
//         private readonly ChangePasswordChallengeProcessor _processor;
//
//         public ChangePasswordChallengeProcessorTests()
//         {
//             _cacheMock = new Mock<ICacheService>();
//             _ldapAdapterMock = new Mock<ILdapAdapter>();
//             _loggerMock = new Mock<ILogger<ChangePasswordChallengeProcessor>>();
//             _processor = new ChangePasswordChallengeProcessor(_cacheMock.Object, _ldapAdapterMock.Object, _loggerMock.Object);
//         }
//
//         [Fact]
//         public void ChallengeType_ShouldReturnPasswordChange()
//         {
//             // Assert
//             Assert.Equal(ChallengeType.PasswordChange, _processor.ChallengeType);
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
//         public void AddChallengeContext_ShouldThrowInvalidOperationException_WhenPasswordIsEmpty()
//         {
//             // Arrange
//             var context = new RadiusPipelineContext(It.IsAny<RadiusPacket>(), It.IsAny<ClientConfiguration>())
//             {
//                 Passphrase = It.IsAny<UserPassphrase>()
//             };
//
//             // Act & Assert
//             Assert.Throws<InvalidOperationException>(() => _processor.AddChallengeContext(context));
//         }
//
//         [Fact]
//         public void AddChallengeContext_ShouldThrowInvalidOperationException_WhenDomainIsEmpty()
//         {
//             // Arrange
//             var context = new RadiusPipelineContext(It.IsAny<RadiusPacket>(), It.IsAny<ClientConfiguration>())
//             {
//                 Passphrase = UserPassphrase.Parse("oldPassword", PreAuthMode.None),
//                 MustChangePasswordDomain = ""
//             };
//
//             // Act & Assert
//             Assert.Throws<InvalidOperationException>(() => _processor.AddChallengeContext(context));
//         }
//
//         [Fact]
//         public void AddChallengeContext_ShouldAddChallengeToCacheAndSetResponse()
//         {
//             // Arrange
//             var clientConfiguration = new ClientConfiguration
//             {
//                 Name = "TestClient",
//                 MultifactorSharedSecret = "sharedSecret"
//             };
//             var context = new RadiusPipelineContext(It.IsAny<RadiusPacket>(), clientConfiguration)
//             {
//                 Passphrase = UserPassphrase.Parse("oldPassword", PreAuthMode.None),
//                 MustChangePasswordDomain = "test.local",
//                 ResponseInformation = new ResponseInformation()
//             };
//
//             PasswordChangeCache capturedCache = null;
//             _cacheMock.Setup(x => x.Set(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<DateTimeOffset>()))
//                 .Callback<string, object, DateTimeOffset>((key, value, expiry) => capturedCache = value as PasswordChangeCache);
//
//             // Act
//             var identifier = _processor.AddChallengeContext(context);
//
//             // Assert
//             Assert.NotNull(capturedCache);
//             Assert.Equal("test.local", capturedCache.Domain);
//             Assert.NotNull(capturedCache.CurrentPasswordEncryptedData);
//             Assert.NotNull(capturedCache.Id);
//             Assert.Equal(capturedCache.Id, context.ResponseInformation.State);
//             Assert.Equal("Please change password to continue. Enter new password: ", context.ResponseInformation.ReplyMessage);
//             Assert.Equal("TestClient", identifier.ToString());
//             Assert.Equal(capturedCache.Id, identifier.RequestId);
//             _cacheMock.Verify(x => x.Set(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<DateTimeOffset>()), Times.Once);
//         }
//
//         [Fact]
//         public void HasChallengeContext_ShouldReturnTrue_WhenCacheHasValue()
//         {
//             // Arrange
//             var requestId = "test-id";
//             _cacheMock.Setup(x => x.TryGetValue<object>(requestId, out It.Ref<object>.IsAny))
//                 .Returns(true);
//
//             var identifier = new ChallengeIdentifier("client", requestId);
//
//             // Act
//             var result = _processor.HasChallengeContext(identifier);
//
//             // Assert
//             Assert.True(result);
//         }
//
//         [Fact]
//         public void HasChallengeContext_ShouldReturnFalse_WhenCacheHasNoValue()
//         {
//             // Arrange
//             var requestId = "test-id";
//             object cacheValue = null;
//             _cacheMock.Setup(x => x.TryGetValue<object>(requestId, out It.Ref<object>.IsAny))
//                 .Callback(new TryGetValueCallback<object>((string key, out object value) => value = null))
//                 .Returns(false);
//
//             var identifier = new ChallengeIdentifier("client", requestId);
//
//             // Act
//             var result = _processor.HasChallengeContext(identifier);
//
//             // Assert
//             Assert.False(result);
//         }
//
//         [Fact]
//         public async Task ProcessChallengeAsync_ShouldThrowArgumentNullException_WhenContextIsNull()
//         {
//             // Arrange
//             var identifier = new ChallengeIdentifier("client", "request-id");
//
//             // Act & Assert
//             await Assert.ThrowsAsync<ArgumentNullException>(() => _processor.ProcessChallengeAsync(identifier, null));
//         }
//
//         [Fact]
//         public async Task ProcessChallengeAsync_ShouldReturnAccept_WhenCacheHasNoRequest()
//         {
//             // Arrange
//             var identifier = new ChallengeIdentifier("client", "request-id");
//             var context = It.IsAny<RadiusPipelineContext>();
//
//             _cacheMock.Setup(x => x.TryGetValue(identifier.RequestId, out It.Ref<PasswordChangeCache>.IsAny))
//                 .Returns(false);
//
//             // Act
//             var result = await _processor.ProcessChallengeAsync(identifier, context);
//
//             // Assert
//             Assert.Equal(ChallengeStatus.Accept, result);
//         }
//
//         [Fact]
//         public async Task ProcessChallengeAsync_ShouldReturnReject_WhenRawPasswordIsEmpty()
//         {
//             // Arrange
//             var identifier = new ChallengeIdentifier("client", "request-id");
//             var context = new RadiusPipelineContext(It.IsAny<RadiusPacket>(), It.IsAny<ClientConfiguration>())
//             {
//                 Passphrase =  It.IsAny<UserPassphrase>(),
//                 LdapProfile = It.IsAny<LdapProfile>()
//             };
//
//             var passwordChangeRequest = new PasswordChangeCache();
//             _cacheMock.Setup(x => x.TryGetValue(identifier.RequestId, out It.Ref<PasswordChangeCache>.IsAny))
//                 .Callback(new TryGetValueCallback<PasswordChangeCache>((string key, out PasswordChangeCache value) => value = passwordChangeRequest))
//                 .Returns(true);
//
//             // Act
//             var result = await _processor.ProcessChallengeAsync(identifier, context);
//
//             // Assert
//             Assert.Equal(ChallengeStatus.Reject, result);
//             Assert.Equal(AuthenticationStatus.Reject, context.FirstFactorStatus);
//             Assert.Equal("Password is empty", context.ResponseInformation.ReplyMessage);
//         }
//
//         [Fact]
//         public async Task ProcessChallengeAsync_ShouldReturnInProcess_WhenNewPasswordNotSet()
//         {
//             // Arrange
//             var identifier = new ChallengeIdentifier("client", "request-id");
//             var context = new RadiusPipelineContext(It.IsAny<RadiusPacket>(), It.IsAny<ClientConfiguration>())
//             {
//                 Passphrase = new UserPassphrase { Raw = "newPass1" },
//                 ClientConfiguration = new ClientConfiguration { MultifactorSharedSecret = "secret" },
//                 LdapProfile = It.IsAny<LdapProfile>(),
//                 LdapSchema = It.IsAny<ILdapSchema>(),
//                 ResponseInformation = new ResponseInformation()
//             };
//
//             var passwordChangeRequest = new PasswordChangeCache { Id = "cache-id" };
//             _cacheMock.Setup(x => x.TryGetValue(identifier.RequestId, out It.Ref<PasswordChangeCache>.IsAny))
//                 .Callback(new TryGetValueCallback<PasswordChangeCache>((string key, out PasswordChangeCache value) => value = passwordChangeRequest))
//                 .Returns(true);
//
//             // Act
//             var result = await _processor.ProcessChallengeAsync(identifier, context);
//
//             // Assert
//             Assert.Equal(ChallengeStatus.InProcess, result);
//             Assert.Equal("cache-id", context.ResponseInformation.State);
//             Assert.Equal("Please repeat new password: ", context.ResponseInformation.ReplyMessage);
//             _cacheMock.Verify(x => x.Set(identifier.RequestId, It.IsAny<PasswordChangeCache>(), It.IsAny<DateTimeOffset>()), Times.Once);
//         }
//
//         [Fact]
//         public async Task ProcessChallengeAsync_ShouldReturnInProcess_WhenPasswordsNotMatch()
//         {
//             // Arrange
//             var identifier = new ChallengeIdentifier("client", "request-id");
//             var context = new RadiusPipelineContext
//             {
//                 Passphrase = new UserPassphrase { Raw = "newPass2" },
//                 ClientConfiguration = new ClientConfiguration { MultifactorSharedSecret = "secret" },
//                 LdapProfile = new LdapProfile(),
//                 LdapConfiguration = new LdapServerConfiguration(),
//                 LdapSchema = It.IsAny<ILdapSchema>(),
//                 ResponseInformation = new ResponseInformation()
//             };
//
//             var encryptedPassword = ProtectionService.Protect("secret", "newPass1");
//             var passwordChangeRequest = new PasswordChangeCache 
//             { 
//                 Id = "cache-id",
//                 NewPasswordEncryptedData = encryptedPassword
//             };
//
//             _cacheMock.Setup(x => x.TryGetValue(identifier.RequestId, out It.Ref<PasswordChangeCache>.IsAny))
//                 .Callback(new TryGetValueCallback<PasswordChangeCache>((string key, out PasswordChangeCache value) => value = passwordChangeRequest))
//                 .Returns(true);
//
//             // Act
//             var result = await _processor.ProcessChallengeAsync(identifier, context);
//
//             // Assert
//             Assert.Equal(ChallengeStatus.InProcess, result);
//             Assert.Equal("cache-id", context.ResponseInformation.State);
//             Assert.Equal("Passwords not match. Please enter new password: ", context.ResponseInformation.ReplyMessage);
//             _cacheMock.Verify(x => x.Set(identifier.RequestId, It.IsAny<PasswordChangeCache>(), It.IsAny<DateTimeOffset>()), Times.Once);
//         }
//
//         [Fact]
//         public async Task ProcessChallengeAsync_ShouldReturnAccept_WhenPasswordChangeSucceeds()
//         {
//             // Arrange
//             var identifier = new ChallengeIdentifier("client", "request-id");
//             var context = new RadiusPipelineContext
//             {
//                 Passphrase = new UserPassphrase { Raw = "newPass1" },
//                 ClientConfiguration = new ClientConfiguration { MultifactorSharedSecret = "secret" },
//                 LdapProfile = new LdapProfile { Dn = "cn=user,dc=test" },
//                 LdapConfiguration = new LdapServerConfiguration()
//                 {
//                     ConnectionString = "ldap://test",
//                     Username = "admin",
//                     Password = "adminPass",
//                     BindTimeoutSeconds = 30
//                 },
//                 LdapSchema = It.IsAny<ILdapSchema>(),
//                 ResponseInformation = new ResponseInformation()
//             };
//
//             var encryptedPassword = ProtectionService.Protect("secret", "newPass1");
//             var passwordChangeRequest = new PasswordChangeCache 
//             { 
//                 Id = "cache-id",
//                 NewPasswordEncryptedData = encryptedPassword
//             };
//
//             _cacheMock.Setup(x => x.TryGetValue(identifier.RequestId, out It.Ref<PasswordChangeCache>.IsAny))
//                 .Callback(new TryGetValueCallback<PasswordChangeCache>((string key, out PasswordChangeCache value) => value = passwordChangeRequest))
//                 .Returns(true);
//
//             _ldapAdapterMock.Setup(x => x.ChangeUserPassword(It.IsAny<ChangeUserPasswordRequest>()))
//                 .Returns(true);
//
//             // Act
//             var result = await _processor.ProcessChallengeAsync(identifier, context);
//
//             // Assert
//             Assert.Equal(ChallengeStatus.Accept, result);
//             Assert.Null(context.ResponseInformation.State);
//             _cacheMock.Verify(x => x.Remove("cache-id"), Times.Once);
//             _ldapAdapterMock.Verify(x => x.ChangeUserPassword(It.IsAny<ChangeUserPasswordRequest>()), Times.Once);
//         }
//
//         [Fact]
//         public async Task ProcessChallengeAsync_ShouldReturnReject_WhenPasswordChangeFails()
//         {
//             // Arrange
//             var identifier = new ChallengeIdentifier("client", "request-id");
//             var context = new RadiusPipelineContext
//             {
//                 Passphrase = new UserPassphrase { Raw = "newPass1" },
//                 ClientConfiguration = new ClientConfiguration { MultifactorSharedSecret = "secret" },
//                 LdapProfile = new LdapProfile { Dn = "cn=user,dc=test" },
//                 LdapConfiguration = new LdapServerConfiguration
//                 {
//                     ConnectionString = "ldap://test",
//                     Username = "admin",
//                     Password = "adminPass",
//                     BindTimeoutSeconds = 30
//                 },
//                 LdapSchema = It.IsAny<ILdapSchema>(),
//                 ResponseInformation = new ResponseInformation()
//             };
//
//             var encryptedPassword = ProtectionService.Protect("secret", "newPass1");
//             var passwordChangeRequest = new PasswordChangeCache 
//             { 
//                 Domain = "cache-id",
//                 NewPasswordEncryptedData = encryptedPassword
//             };
//
//             _cacheMock.Setup(x => x.TryGetValue(identifier.RequestId, out It.Ref<PasswordChangeCache>.IsAny))
//                 .Callback(new TryGetValueCallback<PasswordChangeCache>((string key, out PasswordChangeCache value) => value = passwordChangeRequest))
//                 .Returns(true);
//
//             _ldapAdapterMock.Setup(x => x.ChangeUserPassword(It.IsAny<ChangeUserPasswordRequest>()))
//                 .Returns(false);
//
//             // Act
//             var result = await _processor.ProcessChallengeAsync(identifier, context);
//
//             // Assert
//             Assert.Equal(ChallengeStatus.Reject, result);
//             Assert.Equal(AuthenticationStatus.Reject, context.FirstFactorStatus);
//             _cacheMock.Verify(x => x.Remove("cache-id"), Times.Once);
//         }
//     }
// }