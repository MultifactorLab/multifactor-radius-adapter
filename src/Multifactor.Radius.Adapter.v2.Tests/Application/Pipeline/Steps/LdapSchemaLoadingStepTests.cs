using Microsoft.Extensions.Logging;
using Moq;
using Multifactor.Core.Ldap.Schema;
using Multifactor.Radius.Adapter.v2.Application.Cache;
using Multifactor.Radius.Adapter.v2.Application.Core;
using Multifactor.Radius.Adapter.v2.Application.Features.Ldap.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.Ldap.Ports;
using Multifactor.Radius.Adapter.v2.Application.Features.LoadLdapSchema;
using Multifactor.Radius.Adapter.v2.Application.Features.Radius.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.Radius.Models.Enums;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configurations.Models;

namespace Multifactor.Radius.Adapter.v2.Tests.Application.Pipeline.Steps
{
    public class LdapSchemaLoadingStepTests
    {
        private readonly Mock<ILdapAdapter> _ldapAdapterMock;
        private readonly Mock<ICacheService> _cacheMock;
        private readonly Mock<ILogger<LdapSchemaLoadingStep>> _loggerMock;
        private readonly LdapSchemaLoadingStep _step;

        public LdapSchemaLoadingStepTests()
        {
            _ldapAdapterMock = new Mock<ILdapAdapter>();
            _cacheMock = new Mock<ICacheService>();
            _loggerMock = new Mock<ILogger<LdapSchemaLoadingStep>>();
            
            _step = new LdapSchemaLoadingStep(
                _ldapAdapterMock.Object,
                _cacheMock.Object,
                _loggerMock.Object);
        }

        [Fact]
        public async Task ExecuteAsync_WhenContextIsNull_ThrowsException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => 
                _step.ExecuteAsync(null));
        }

        [Fact]
        public async Task ExecuteAsync_WhenLdapConfigurationIsNull_ThrowsException()
        {
            // Arrange
            var context = CreateTestContextWithoutLdap();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => 
                _step.ExecuteAsync(context));
        }

        [Fact]
        public async Task ExecuteAsync_WhenSchemaInCache_UsesCachedSchema()
        {
            // Arrange
            var context = CreateTestContext();
            var cachedSchema = new Mock<ILdapSchema>().Object;
            
            _cacheMock
                .Setup(x => x.TryGetValue("ldap://test.com", out cachedSchema))
                .Returns(true);

            // Act
            await _step.ExecuteAsync(context);

            // Assert
            Assert.Same(cachedSchema, context.LdapSchema);
            _ldapAdapterMock.Verify(
                x => x.LoadSchema(It.IsAny<LdapConnectionData>()),
                Times.Never);
            
            VerifyDebugLog("from cache");
        }

        [Fact]
        public async Task ExecuteAsync_WhenSchemaNotInCache_LoadsFromLdap()
        {
            // Arrange
            var context = CreateTestContext();
            var loadedSchema = new Mock<ILdapSchema>().Object;
            
            _cacheMock
                .Setup(x => x.TryGetValue("ldap://test.com", out It.Ref<ILdapSchema>.IsAny))
                .Returns(false);
            
            _ldapAdapterMock
                .Setup(x => x.LoadSchema(It.Is<LdapConnectionData>(d => 
                    d.ConnectionString == "ldap://test.com" &&
                    d.UserName == "admin" &&
                    d.Password == "password" &&
                    d.BindTimeoutInSeconds == 30)))
                .Returns(loadedSchema);

            // Act
            await _step.ExecuteAsync(context);

            // Assert
            Assert.Same(loadedSchema, context.LdapSchema);
            _cacheMock.Verify(
                x => x.Set(
                    "ldap://test.com", 
                    loadedSchema, 
                    It.Is<DateTimeOffset>(d => d > DateTimeOffset.Now)),
                Times.Once);
            
            VerifyDebugLog("saved in cache");
        }

        [Fact]
        public async Task ExecuteAsync_WhenSchemaLoadFails_ThrowsException()
        {
            // Arrange
            var context = CreateTestContext();
            
            _cacheMock
                .Setup(x => x.TryGetValue("ldap://test.com", out It.Ref<ILdapSchema>.IsAny))
                .Returns(false);
            
            _ldapAdapterMock
                .Setup(x => x.LoadSchema(It.IsAny<LdapConnectionData>()))
                .Returns((ILdapSchema?)null);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => 
                _step.ExecuteAsync(context));
            
            VerifyWarningLog();
        }

        [Fact]
        public async Task ExecuteAsync_WhenSchemaLoaded_SetsCacheForOneHour()
        {
            // Arrange
            var context = CreateTestContext();
            var loadedSchema = new Mock<ILdapSchema>().Object;
            DateTimeOffset? cachedExpiration = null;
            
            _cacheMock
                .Setup(x => x.TryGetValue("ldap://test.com", out It.Ref<ILdapSchema>.IsAny))
                .Returns(false);
            
            _ldapAdapterMock
                .Setup(x => x.LoadSchema(It.IsAny<LdapConnectionData>()))
                .Returns(loadedSchema);
            
            _cacheMock
                .Setup(x => x.Set(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<DateTimeOffset>()))
                .Callback<string, object, DateTimeOffset>((key, value, expiration) =>
                {
                    cachedExpiration = expiration;
                });

            // Act
            await _step.ExecuteAsync(context);

            // Assert
            Assert.NotNull(cachedExpiration);
            var expectedExpiration = DateTimeOffset.Now.AddHours(1);
            Assert.True(cachedExpiration.Value > DateTimeOffset.Now && 
                       cachedExpiration.Value <= expectedExpiration);
        }

        [Fact]
        public async Task ExecuteAsync_LogsDebugOnStart()
        {
            // Arrange
            var context = CreateTestContext();
            var cachedSchema = new Mock<ILdapSchema>().Object;
            
            _cacheMock
                .Setup(x => x.TryGetValue("ldap://test.com", out cachedSchema))
                .Returns(true);

            var logMessages = new List<string>();
            SetupLogCapture(logMessages);

            // Act
            await _step.ExecuteAsync(context);

            // Assert
            Assert.Contains(logMessages, msg => 
                msg.Contains(nameof(LdapSchemaLoadingStep)) && 
                msg.Contains("started"));
        }

        [Fact]
        public async Task ExecuteAsync_WithDifferentConnectionString_UsesItAsCacheKey()
        {
            // Arrange
            var context = CreateTestContext();
            // context.LdapConfiguration.ConnectionString = "ldap://another-server:389";
            var cachedSchema = new Mock<ILdapSchema>().Object;
            
            _cacheMock
                .Setup(x => x.TryGetValue("ldap://another-server:389", out cachedSchema))
                .Returns(true);

            // Act
            await _step.ExecuteAsync(context);

            // Assert
            _cacheMock.Verify(
                x => x.TryGetValue("ldap://another-server:389", out cachedSchema),
                Times.Once);
        }

        private void SetupLogCapture(List<string> logMessages)
        {
            _loggerMock.Setup(x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()))
                .Callback<LogLevel, EventId, object, Exception, Func<object, Exception?, string>>(
                    (level, eventId, state, exception, formatter) =>
                    {
                        logMessages.Add(formatter(state, exception));
                    });
        }

        private void VerifyDebugLog(string expectedMessage)
        {
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Debug,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains(expectedMessage)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        private void VerifyWarningLog()
        {
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Unable to load")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        private static RadiusPipelineContext CreateTestContext()
        {
            var clientConfig = new ClientConfiguration
            {
                Name = "test-client",
                RadiusSharedSecret = "test-secret"
            };

            var ldapConfig = new LdapServerConfiguration
            {
                ConnectionString = "ldap://test.com",
                Username = "admin",
                Password = "password",
                BindTimeoutSeconds = 30
            };

            var requestPacket = new RadiusPacket(
                new RadiusPacketHeader(PacketCode.AccessRequest, 1, new byte[16]));

            var context = new RadiusPipelineContext(requestPacket, clientConfig, ldapConfig);
            return context;
        }

        private static RadiusPipelineContext CreateTestContextWithoutLdap()
        {
            var clientConfig = new ClientConfiguration
            {
                Name = "test-client",
                RadiusSharedSecret = "test-secret"
            };

            var requestPacket = new RadiusPacket(
                new RadiusPacketHeader(PacketCode.AccessRequest, 1, new byte[16]));

            var context = new RadiusPipelineContext(requestPacket, clientConfig);
            return context;
        }
    }
}