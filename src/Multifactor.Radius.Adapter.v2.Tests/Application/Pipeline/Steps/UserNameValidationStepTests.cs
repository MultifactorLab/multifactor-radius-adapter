using Microsoft.Extensions.Logging;
using Moq;
using Multifactor.Radius.Adapter.v2.Application.Configuration.Models;
using Multifactor.Radius.Adapter.v2.Application.Core;
using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.Models.Enum;
using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.Steps;
using Multifactor.Radius.Adapter.v2.Application.Features.Radius.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.Radius.Models.Enums;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configurations.Models;

namespace Multifactor.Radius.Adapter.v2.Tests.Application.Pipeline.Steps
{
    public class UserNameValidationStepTests
    {
        private readonly Mock<ILogger<UserNameValidationStep>> _loggerMock;
        private readonly UserNameValidationStep _step;

        public UserNameValidationStepTests()
        {
            _loggerMock = new Mock<ILogger<UserNameValidationStep>>();
            _step = new UserNameValidationStep(_loggerMock.Object);
        }

        [Fact]
        public async Task ExecuteAsync_ShouldSkipWhenNoLdapConfiguration()
        {
            // Arrange
            var requestPacket = CreateRadiusPacket("user@domain.com");
            var context = CreateContext(requestPacket, null);

            // Act
            await _step.ExecuteAsync(context);

            // Assert
            Assert.False(context.IsTerminated);
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Debug,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("No LDAP server configuration")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_ShouldTerminateWhenRequiresUpnAndNotUpn()
        {
            // Arrange
            var requestPacket = CreateRadiusPacket("DOMAIN\\user"); // NetBIOS, не UPN
            var ldapConfig = new LdapServerConfiguration { RequiresUpn = true };
            var context = CreateContext(requestPacket, ldapConfig);

            // Act
            await _step.ExecuteAsync(context);

            // Assert
            Assert.True(context.IsTerminated);
            Assert.Equal(AuthenticationStatus.Reject, context.FirstFactorStatus);
            Assert.Equal("User name in UPN format is required.", context.ResponseInformation.ReplyMessage);
            
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("User name in UPN format is required")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Theory]
        [InlineData("user@domain.com", true)] // Включенный суффикс
        [InlineData("user@other.com", false)] // Исключенный суффикс
        public async Task ExecuteAsync_ShouldValidateIncludedSuffixes(string userName, bool shouldPass)
        {
            // Arrange
            var requestPacket = CreateRadiusPacket(userName);
            var ldapConfig = new LdapServerConfiguration
            {
                IncludedSuffixes = new List<string> { "domain.com", "example.com" }
            };
            var context = CreateContext(requestPacket, ldapConfig);

            // Act
            await _step.ExecuteAsync(context);

            // Assert
            if (shouldPass)
            {
                Assert.False(context.IsTerminated);
            }
            else
            {
                Assert.True(context.IsTerminated);
                Assert.Equal("UPN suffix is not permitted.", context.ResponseInformation.ReplyMessage);
            }
        }

        [Theory]
        [InlineData("user@domain.com", false)] // Исключенный суффикс
        [InlineData("user@other.com", true)] // Не исключенный суффикс
        public async Task ExecuteAsync_ShouldValidateExcludedSuffixes(string userName, bool shouldPass)
        {
            // Arrange
            var requestPacket = CreateRadiusPacket(userName);
            var ldapConfig = new LdapServerConfiguration
            {
                ExcludedSuffixes = new List<string> { "domain.com", "example.com" }
            };
            var context = CreateContext(requestPacket, ldapConfig);

            // Act
            await _step.ExecuteAsync(context);

            // Assert
            if (shouldPass)
            {
                Assert.False(context.IsTerminated);
            }
            else
            {
                Assert.True(context.IsTerminated);
                Assert.Equal("UPN suffix is not permitted.", context.ResponseInformation.ReplyMessage);
            }
        }

        [Fact]
        public async Task ExecuteAsync_ShouldAllowWhenNoSuffixRestrictions()
        {
            // Arrange
            var requestPacket = CreateRadiusPacket("user@anydomain.com");
            var ldapConfig = new LdapServerConfiguration
            {
                IncludedSuffixes = new List<string>(),
                ExcludedSuffixes = new List<string>()
            };
            var context = CreateContext(requestPacket, ldapConfig);

            // Act
            await _step.ExecuteAsync(context);

            // Assert
            Assert.False(context.IsTerminated);
        }

        [Fact]
        public async Task ExecuteAsync_ShouldHandleEmptyUserName()
        {
            // Arrange
            var requestPacket = CreateRadiusPacket(null);
            var ldapConfig = new LdapServerConfiguration();
            var context = CreateContext(requestPacket, ldapConfig);

            // Act
            await _step.ExecuteAsync(context);

            // Assert
            Assert.False(context.IsTerminated);
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Debug,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("User name is empty")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        private RadiusPacket CreateRadiusPacket(string userName)
        {
            var header = new RadiusPacketHeader(PacketCode.AccessRequest, 1, new byte[16]);
            var packet = new RadiusPacket(header);
            
            if (userName != null)
            {
                packet.AddAttributeValue("User-Name", userName);
            }
            
            return packet;
        }

        private RadiusPipelineContext CreateContext(RadiusPacket requestPacket, ILdapServerConfiguration ldapConfig)
        {
            var clientConfig = new ClientConfiguration { Name = "TestClient" };
            var context = new RadiusPipelineContext(requestPacket, clientConfig, ldapConfig);
            return context;
        }
    }
}