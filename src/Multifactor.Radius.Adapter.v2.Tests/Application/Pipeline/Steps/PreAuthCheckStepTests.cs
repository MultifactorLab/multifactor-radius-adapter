using System.Net;
using Microsoft.Extensions.Logging;
using Moq;
using Multifactor.Radius.Adapter.v2.Application.Configuration.Models;
using Multifactor.Radius.Adapter.v2.Application.Core;
using Multifactor.Radius.Adapter.v2.Application.Core.Models.Enum;
using Multifactor.Radius.Adapter.v2.Application.Features.Ldap.Ports;
using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.Models.Enum;
using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.Steps;
using Multifactor.Radius.Adapter.v2.Application.Features.Radius.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.Radius.Models.Enums;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configurations.Models;

namespace Multifactor.Radius.Adapter.v2.Tests.Application.Pipeline.Steps
{
    public class PreAuthCheckStepTests
    {
        private readonly Mock<ILogger<PreAuthCheckStep>> _loggerMock;
        private readonly Mock<ILdapAdapter> _ldapAdapterMock;
        private readonly PreAuthCheckStep _step;

        public PreAuthCheckStepTests()
        {
            _loggerMock = new Mock<ILogger<PreAuthCheckStep>>();
            _ldapAdapterMock = new Mock<ILdapAdapter>();
            
            _step = new PreAuthCheckStep(_loggerMock.Object, _ldapAdapterMock.Object);
        }

        [Fact]
        public async Task ExecuteAsync_ShouldTerminateWhenOtpRequiredButMissing()
        {
            // Arrange
            var requestPacket = CreateRadiusPacket("testuser", "password"); // Нет OTP
            var clientConfig = new ClientConfiguration
            {
                Name = "TestClient",
                PreAuthenticationMethod = PreAuthMode.Otp
            };
            var context = CreateContext(requestPacket, clientConfig);

            // Act
            await _step.ExecuteAsync(context);

            // Assert
            Assert.True(context.IsTerminated);
            Assert.Equal(AuthenticationStatus.Reject, context.SecondFactorStatus);
            
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("otp code is empty")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_ShouldContinueWhenOtpRequiredAndPresent()
        {
            // Arrange
            var requestPacket = CreateRadiusPacket("testuser", "password1234567890"); // С OTP
            var clientConfig = new ClientConfiguration
            {
                Name = "TestClient",
                PreAuthenticationMethod = PreAuthMode.Otp
            };
            var context = CreateContext(requestPacket, clientConfig);

            // Act
            await _step.ExecuteAsync(context);

            // Assert
            Assert.False(context.IsTerminated);
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Debug,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Pre-auth check")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_ShouldContinueWhenPreAuthModeIsNone()
        {
            // Arrange
            var requestPacket = CreateRadiusPacket("testuser", "password");
            var clientConfig = new ClientConfiguration
            {
                Name = "TestClient",
                PreAuthenticationMethod = PreAuthMode.None
            };
            var context = CreateContext(requestPacket, clientConfig);

            // Act
            await _step.ExecuteAsync(context);

            // Assert
            Assert.False(context.IsTerminated);
        }

        [Fact]
        public async Task ExecuteAsync_ShouldContinueWhenPreAuthModeIsAny()
        {
            // Arrange
            var requestPacket = CreateRadiusPacket("testuser", "password");
            var clientConfig = new ClientConfiguration
            {
                Name = "TestClient",
                PreAuthenticationMethod = PreAuthMode.Any
            };
            var context = CreateContext(requestPacket, clientConfig);

            // Act
            await _step.ExecuteAsync(context);

            // Assert
            Assert.False(context.IsTerminated);
        }

        [Fact]
        public async Task ExecuteAsync_ShouldThrowOnUnknownPreAuthMethod()
        {
            // Arrange
            var requestPacket = CreateRadiusPacket("testuser", "password");
            var clientConfig = new ClientConfiguration
            {
                Name = "TestClient",
                PreAuthenticationMethod = (PreAuthMode)999 // Неизвестный метод
            };
            var context = CreateContext(requestPacket, clientConfig);

            // Act & Assert
            await Assert.ThrowsAsync<NotImplementedException>(
                () => _step.ExecuteAsync(context));
        }

        private RadiusPacket CreateRadiusPacket(string userName, string password)
        {
            var header = new RadiusPacketHeader(PacketCode.AccessRequest, 1, new byte[16]);
            var packet = new RadiusPacket(header)
            {
                RemoteEndpoint = new IPEndPoint(IPAddress.Any, 228)
            };
            
            packet.AddAttributeValue("User-Name", userName);
            packet.AddAttributeValue("User-Password", password);
            
            return packet;
        }

        private RadiusPipelineContext CreateContext(RadiusPacket requestPacket, IClientConfiguration clientConfig)
        {
            var context = new RadiusPipelineContext(requestPacket, clientConfig);
            
            var passphrase = UserPassphrase.Parse(
                requestPacket.TryGetUserPassword(),
                clientConfig.PreAuthenticationMethod ?? PreAuthMode.None);
            
            context.Passphrase = passphrase;
            
            return context;
        }
    }
}