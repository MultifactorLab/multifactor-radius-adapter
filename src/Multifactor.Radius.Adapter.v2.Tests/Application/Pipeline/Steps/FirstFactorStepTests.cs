using Microsoft.Extensions.Logging;
using Moq;
using Multifactor.Radius.Adapter.v2.Application.Configuration.Models;
using Multifactor.Radius.Adapter.v2.Application.Core;
using Multifactor.Radius.Adapter.v2.Application.Core.Models.Enum;
using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.AccessChallenge;
using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.AccessChallenge.Models.Enums;
using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.FirstFactor;
using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.Models.Enum;
using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.Steps;
using Multifactor.Radius.Adapter.v2.Application.Features.Radius.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.Radius.Models.Enums;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configurations.Models;

namespace Multifactor.Radius.Adapter.v2.Tests.Application.Pipeline.Steps
{
    public class FirstFactorStepTests
    {
        private readonly Mock<IFirstFactorProcessorProvider> _processorProviderMock;
        private readonly Mock<IChallengeProcessorProvider> _challengeProviderMock;
        private readonly Mock<ILogger<FirstFactorStep>> _loggerMock;
        private readonly FirstFactorStep _step;

        public FirstFactorStepTests()
        {
            _processorProviderMock = new Mock<IFirstFactorProcessorProvider>();
            _challengeProviderMock = new Mock<IChallengeProcessorProvider>();
            _loggerMock = new Mock<ILogger<FirstFactorStep>>();
            
            _step = new FirstFactorStep(
                _processorProviderMock.Object,
                _challengeProviderMock.Object,
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
        public async Task ExecuteAsync_WhenStatusNotAwaiting_DoesNothing()
        {
            // Arrange
            var clientConfiguration = CreateTestClientConfiguration();
            var context = CreateTestContext(clientConfiguration);
            context.FirstFactorStatus = AuthenticationStatus.Accept; // Not Awaiting

            // Act
            await _step.ExecuteAsync(context);

            // Assert
            _processorProviderMock.Verify(
                x => x.GetProcessor(It.IsAny<AuthenticationSource>()),
                Times.Never);
        }

        [Fact]
        public async Task ExecuteAsync_WhenStatusAwaiting_GetsAndRunsProcessor()
        {
            // Arrange
            var clientConfiguration = CreateTestClientConfiguration();
            var context = CreateTestContext(clientConfiguration);
            context.FirstFactorStatus = AuthenticationStatus.Awaiting;

            var processorMock = new Mock<IFirstFactorProcessor>();
            _processorProviderMock
                .Setup(x => x.GetProcessor(AuthenticationSource.Radius))
                .Returns(processorMock.Object);

            // Act
            await _step.ExecuteAsync(context);

            // Assert
            _processorProviderMock.Verify(
                x => x.GetProcessor(AuthenticationSource.Radius),
                Times.Once);
            
            processorMock.Verify(
                x => x.ProcessFirstFactor(context),
                Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_WhenMustChangePasswordDomainEmpty_NoChallenge()
        {
            // Arrange
            var clientConfiguration = CreateTestClientConfiguration();
            var context = CreateTestContext(clientConfiguration);
            context.FirstFactorStatus = AuthenticationStatus.Awaiting;
            context.MustChangePasswordDomain = ""; // Empty

            var processorMock = new Mock<IFirstFactorProcessor>();
            _processorProviderMock
                .Setup(x => x.GetProcessor(It.IsAny<AuthenticationSource>()))
                .Returns(processorMock.Object);

            // Act
            await _step.ExecuteAsync(context);

            // Assert
            _challengeProviderMock.Verify(
                x => x.GetChallengeProcessorByType(ChallengeType.PasswordChange),
                Times.Never);
        }

        [Fact]
        public async Task ExecuteAsync_WhenMustChangePasswordDomainSet_CreatesChallenge()
        {
            // Arrange
            var clientConfiguration = CreateTestClientConfiguration();
            var context = CreateTestContext(clientConfiguration);
            context.FirstFactorStatus = AuthenticationStatus.Awaiting;
            context.MustChangePasswordDomain = "test-domain";

            var processorMock = new Mock<IFirstFactorProcessor>();
            _processorProviderMock
                .Setup(x => x.GetProcessor(It.IsAny<AuthenticationSource>()))
                .Returns(processorMock.Object);

            var challengeProcessorMock = new Mock<IChallengeProcessor>();
            _challengeProviderMock
                .Setup(x => x.GetChallengeProcessorByType(ChallengeType.PasswordChange))
                .Returns(challengeProcessorMock.Object);

            // Act
            await _step.ExecuteAsync(context);

            // Assert
            _challengeProviderMock.Verify(
                x => x.GetChallengeProcessorByType(ChallengeType.PasswordChange),
                Times.Once);
            
            challengeProcessorMock.Verify(
                x => x.AddChallengeContext(context),
                Times.Once);
            
            Assert.Equal(AuthenticationStatus.Awaiting, context.FirstFactorStatus);
        }

        [Fact]
        public async Task ExecuteAsync_WhenChallengeProcessorNotFound_ThrowsException()
        {
            // Arrange
            var clientConfiguration = CreateTestClientConfiguration();
            var context = CreateTestContext(clientConfiguration);
            context.FirstFactorStatus = AuthenticationStatus.Awaiting;
            context.MustChangePasswordDomain = "test-domain";

            var processorMock = new Mock<IFirstFactorProcessor>();
            _processorProviderMock
                .Setup(x => x.GetProcessor(It.IsAny<AuthenticationSource>()))
                .Returns(processorMock.Object);

            _challengeProviderMock
                .Setup(x => x.GetChallengeProcessorByType(ChallengeType.PasswordChange))
                .Returns((IChallengeProcessor?)null);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() => 
                _step.ExecuteAsync(context));
            
            Assert.Contains("Challenge processor", exception.Message);
        }

        [Fact]
        public async Task ExecuteAsync_WhenFirstFactorReject_Terminates()
        {
            // Arrange
            var clientConfiguration = CreateTestClientConfiguration();
            var context = CreateTestContext(clientConfiguration);
            context.FirstFactorStatus = AuthenticationStatus.Awaiting;

            var processorMock = new Mock<IFirstFactorProcessor>();
            processorMock.Setup(x => x.ProcessFirstFactor(context))
                .Callback(() => context.FirstFactorStatus = AuthenticationStatus.Reject);
            
            _processorProviderMock
                .Setup(x => x.GetProcessor(It.IsAny<AuthenticationSource>()))
                .Returns(processorMock.Object);

            // Act
            await _step.ExecuteAsync(context);

            // Assert
            Assert.True(context.IsTerminated);
        }

        [Fact]
        public async Task ExecuteAsync_WhenFirstFactorAccept_DoesNotTerminate()
        {
            // Arrange
            var clientConfiguration = CreateTestClientConfiguration();
            var context = CreateTestContext(clientConfiguration);
            context.FirstFactorStatus = AuthenticationStatus.Awaiting;

            var processorMock = new Mock<IFirstFactorProcessor>();
            processorMock.Setup(x => x.ProcessFirstFactor(context))
                .Callback(() => context.FirstFactorStatus = AuthenticationStatus.Accept);
            
            _processorProviderMock
                .Setup(x => x.GetProcessor(It.IsAny<AuthenticationSource>()))
                .Returns(processorMock.Object);

            // Act
            await _step.ExecuteAsync(context);

            // Assert
            Assert.False(context.IsTerminated);
        }

        [Fact]
        public async Task ExecuteAsync_LogsDebugMessage()
        {
            // Arrange
            var clientConfiguration = CreateTestClientConfiguration();
            var context = CreateTestContext(clientConfiguration);
            context.FirstFactorStatus = AuthenticationStatus.Awaiting;

            var processorMock = new Mock<IFirstFactorProcessor>();
            _processorProviderMock
                .Setup(x => x.GetProcessor(It.IsAny<AuthenticationSource>()))
                .Returns(processorMock.Object);

            var logMessages = new List<string>();
            _loggerMock.Setup(x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()))
                .Callback<LogLevel, EventId, object, Exception, Func<object, Exception?, string>>(
                    (level, eventId, state, exception, formatter) =>
                    {
                        if (level == LogLevel.Debug)
                            logMessages.Add(formatter(state, exception));
                    });

            // Act
            await _step.ExecuteAsync(context);

            // Assert
            Assert.Contains(logMessages, msg => 
                msg.Contains(nameof(FirstFactorStep)) && 
                msg.Contains("started"));
        }

        [Fact]
        public async Task ExecuteAsync_WithLdapSource_GetsCorrectProcessor()
        {
            // Arrange
            var clientConfiguration = CreateTestClientConfiguration(AuthenticationSource.Ldap);
            var context = CreateTestContext(clientConfiguration);
            context.FirstFactorStatus = AuthenticationStatus.Awaiting;

            var processorMock = new Mock<IFirstFactorProcessor>();
            _processorProviderMock
                .Setup(x => x.GetProcessor(AuthenticationSource.Ldap))
                .Returns(processorMock.Object);

            // Act
            await _step.ExecuteAsync(context);

            // Assert
            _processorProviderMock.Verify(
                x => x.GetProcessor(AuthenticationSource.Ldap),
                Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_WithNoneSource_GetsCorrectProcessor()
        {
            // Arrange
            var clientConfiguration = CreateTestClientConfiguration(AuthenticationSource.None);
            var context = CreateTestContext(clientConfiguration);
            context.FirstFactorStatus = AuthenticationStatus.Awaiting;

            var processorMock = new Mock<IFirstFactorProcessor>();
            _processorProviderMock
                .Setup(x => x.GetProcessor(AuthenticationSource.None))
                .Returns(processorMock.Object);

            // Act
            await _step.ExecuteAsync(context);

            // Assert
            _processorProviderMock.Verify(
                x => x.GetProcessor(AuthenticationSource.None),
                Times.Once);
        }

        private static IClientConfiguration CreateTestClientConfiguration(AuthenticationSource source = AuthenticationSource.Radius)
        {
            return new ClientConfiguration
            {
                Name = "test-client",
                RadiusSharedSecret = "test-secret",
                FirstFactorAuthenticationSource = AuthenticationSource.Radius
            };
        }

        private static RadiusPipelineContext CreateTestContext(ClientConfiguration clientConfiguration)
        {
            var requestPacket = new RadiusPacket(
                new RadiusPacketHeader(PacketCode.AccessRequest, 1, new byte[16]));

            return new RadiusPipelineContext(requestPacket, clientConfiguration);
        }
    }
}