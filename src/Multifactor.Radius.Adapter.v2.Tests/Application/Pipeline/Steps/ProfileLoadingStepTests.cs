using System.Net;
using Microsoft.Extensions.Logging;
using Moq;
using Multifactor.Core.Ldap.Name;
using Multifactor.Core.Ldap.Schema;
using Multifactor.Radius.Adapter.v2.Application.Cache;
using Multifactor.Radius.Adapter.v2.Application.Configuration.Models;
using Multifactor.Radius.Adapter.v2.Application.Core;
using Multifactor.Radius.Adapter.v2.Application.Features.Ldap.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.Ldap.Ports;
using Multifactor.Radius.Adapter.v2.Application.Features.LoadProfile;
using Multifactor.Radius.Adapter.v2.Application.Features.Radius.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.Radius.Models.Enums;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configurations.Models;

namespace Multifactor.Radius.Adapter.v2.Tests.Application.Pipeline.Steps
{
    public class ProfileLoadingStepTests
    {
        private readonly Mock<ILdapAdapter> _ldapAdapterMock;
        private readonly Mock<ICacheService> _cacheMock;
        private readonly Mock<ILogger<ProfileLoadingStep>> _loggerMock;
        private readonly ProfileLoadingStep _step;

        public ProfileLoadingStepTests()
        {
            _ldapAdapterMock = new Mock<ILdapAdapter>();
            _cacheMock = new Mock<ICacheService>();
            _loggerMock = new Mock<ILogger<ProfileLoadingStep>>();
            
            _step = new ProfileLoadingStep(
                _ldapAdapterMock.Object,
                _cacheMock.Object,
                _loggerMock.Object);
        }

        [Fact]
        public async Task ExecuteAsync_WhenLocalAccount_SkipsStep()
        {
            // Arrange
            var ldapConfig = CreateTestLdapServerConfiguration();
            var context = CreateTestContext(ldapConfig);
            context.RequestPacket.AddAttributeValue("Acct-Authentic", new[]{2});

            // Act
            await _step.ExecuteAsync(context);

            // Assert
            Assert.Null(context.LdapProfile);
            _cacheMock.Verify(x => x.TryGetValue(It.IsAny<string>(), out It.Ref<ILdapProfile>.IsAny), Times.Never);
            _ldapAdapterMock.Verify(x => x.FindUserProfile(It.IsAny<FindUserRequest>()), Times.Never);
        }

        [Fact]
        public async Task ExecuteAsync_WhenUserNameEmpty_SkipsStep()
        {
            // Arrange
            var ldapConfig = CreateTestLdapServerConfiguration();
            var context = CreateTestContext(ldapConfig);
            context.RequestPacket.ReplaceAttribute("User-Name", "");

            // Act
            await _step.ExecuteAsync(context);

            // Assert
            Assert.Null(context.LdapProfile);
        }

        [Fact]
        public async Task ExecuteAsync_WhenLdapSchemaNull_SkipsStep()
        {
            // Arrange
            var ldapConfig = CreateTestLdapServerConfiguration();
            var context = CreateTestContext(ldapConfig);
            context.LdapSchema = null;

            // Act
            await _step.ExecuteAsync(context);

            // Assert
            Assert.Null(context.LdapProfile);
        }

        [Fact]
        public async Task ExecuteAsync_WhenProfileInCache_UsesCachedProfile()
        {
            // Arrange
            var ldapConfig = CreateTestLdapServerConfiguration();
            var context = CreateTestContext(ldapConfig);
            var cachedProfile = new Mock<ILdapProfile>().Object;
            
            _cacheMock
                .Setup(x => x.TryGetValue("testuser-DC=test,DC=com", out cachedProfile))
                .Returns(true);

            // Act
            await _step.ExecuteAsync(context);

            // Assert
            Assert.Same(cachedProfile, context.LdapProfile);
            _ldapAdapterMock.Verify(x => x.FindUserProfile(It.IsAny<FindUserRequest>()), Times.Never);
        }

        [Fact]
        public async Task ExecuteAsync_WhenProfileNotInCache_LoadsFromLdap()
        {
            // Arrange
            var ldapConfig = CreateTestLdapServerConfiguration();
            var context = CreateTestContext(ldapConfig);
            var loadedProfile = new Mock<ILdapProfile>().Object;
            
            _cacheMock
                .Setup(x => x.TryGetValue("testuser-DC=test,DC=com", out It.Ref<ILdapProfile>.IsAny))
                .Returns(false);
            
            _ldapAdapterMock
                .Setup(x => x.FindUserProfile(It.IsAny<FindUserRequest>()))
                .Returns(loadedProfile);

            // Act
            await _step.ExecuteAsync(context);

            // Assert
            Assert.Same(loadedProfile, context.LdapProfile);
            _cacheMock.Verify(
                x => x.Set("testuser-DC=test,DC=com", loadedProfile, It.IsAny<DateTimeOffset>()),
                Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_WhenProfileNotFound_ThrowsException()
        {
            // Arrange
            var ldapConfig = CreateTestLdapServerConfiguration();
            var context = CreateTestContext(ldapConfig);
            
            _cacheMock
                .Setup(x => x.TryGetValue("testuser-DC=test,DC=com", out It.Ref<ILdapProfile>.IsAny))
                .Returns(false);
            
            _ldapAdapterMock
                .Setup(x => x.FindUserProfile(It.IsAny<FindUserRequest>()))
                .Returns((ILdapProfile?)null);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => 
                _step.ExecuteAsync(context));
        }

        [Fact]
        public async Task ExecuteAsync_IncludesCorrectAttributes()
        {
            // Arrange
            var ldapConfig = CreateTestLdapServerConfiguration();
            var context = CreateTestContext(ldapConfig);
            
            // Add some reply attributes
            var replyAttribute = new RadiusReplyAttribute { Name = "department" };
            context.ClientConfiguration.ReplyAttributes = new Dictionary<string, IRadiusReplyAttribute>
            {
                ["TestAttribute"] = [replyAttribute]
            };

            var loadedProfile = new Mock<ILdapProfile>().Object;
            
            _cacheMock
                .Setup(x => x.TryGetValue(It.IsAny<string>(), out It.Ref<ILdapProfile>.IsAny))
                .Returns(false);
            
            _ldapAdapterMock
                .Setup(x => x.FindUserProfile(It.Is<FindUserRequest>(r => 
                    r.AttributeNames != null &&
                    r.AttributeNames.Any(a => a.Value == "memberOf") &&
                    r.AttributeNames.Any(a => a.Value == "userPrincipalName") &&
                    r.AttributeNames.Any(a => a.Value == "phone") &&
                    r.AttributeNames.Any(a => a.Value == "mail") &&
                    r.AttributeNames.Any(a => a.Value == "displayName") &&
                    r.AttributeNames.Any(a => a.Value == "email") &&
                    r.AttributeNames.Any(a => a.Value == "customId") &&
                    r.AttributeNames.Any(a => a.Value == "department") &&
                    r.AttributeNames.Any(a => a.Value == "mobile") &&
                    r.AttributeNames.Any(a => a.Value == "homePhone"))))
                .Returns(loadedProfile);

            // Act
            await _step.ExecuteAsync(context);

            // Assert
            _ldapAdapterMock.Verify(
                x => x.FindUserProfile(It.IsAny<FindUserRequest>()),
                Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_WithDifferentUserName_CreatesCorrectCacheKey()
        {
            // Arrange
            var ldapConfig = CreateTestLdapServerConfiguration();
            var context = CreateTestContext(ldapConfig);
            context.RequestPacket.ReplaceAttribute("User-Name", "another.user@domain.com");
            
            var cachedProfile = new Mock<ILdapProfile>().Object;
            
            _cacheMock
                .Setup(x => x.TryGetValue("another.user@domain.com-DC=test,DC=com", out cachedProfile))
                .Returns(true);

            // Act
            await _step.ExecuteAsync(context);

            // Assert
            Assert.Same(cachedProfile, context.LdapProfile);
        }

        private static LdapServerConfiguration CreateTestLdapServerConfiguration(string identityAttribute = "", string[]? phoneAttributes = null)
        {
            return new LdapServerConfiguration
            {
                ConnectionString = "ldap://test.com",
                Username = "admin",
                Password = "password",
                BindTimeoutSeconds = 30,
                IdentityAttribute = identityAttribute,
                PhoneAttributes = phoneAttributes ?? []
            };
        }
        private static RadiusPipelineContext CreateTestContext(LdapServerConfiguration?  ldapServerConfiguration = null)
        {
            var clientConfig = new ClientConfiguration
            {
                Name = "test-client",
                RadiusSharedSecret = "test-secret",
                ReplyAttributes = new Dictionary<string, IReadOnlyList<IRadiusReplyAttribute>>()
            };
            var requestPacket = new RadiusPacket(
                new RadiusPacketHeader(PacketCode.AccessRequest, 1, new byte[16]))
            {
                RemoteEndpoint = new IPEndPoint(IPAddress.Any, 228),
            };
            requestPacket.AddAttributeValue("User-Name", "testuser");

            var ldapSchemaMock = new Mock<ILdapSchema>();
            ldapSchemaMock.Setup(x => x.NamingContext).Returns(new DistinguishedName("DC=test,DC=com"));
            var ldapProfileMock = new Mock<ILdapProfile>();

            var context = new RadiusPipelineContext(requestPacket, clientConfig, ldapServerConfiguration)
            {
                LdapSchema = ldapSchemaMock.Object,
                LdapProfile = ldapProfileMock.Object
            };

            return context;
        }
    }
}