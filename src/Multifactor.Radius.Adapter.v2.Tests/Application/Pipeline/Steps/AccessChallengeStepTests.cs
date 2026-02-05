using Microsoft.Extensions.Logging;
using Moq;
using Multifactor.Radius.Adapter.v2.Application.Configuration.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.AccessChallenge;
using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.AccessChallenge.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.AccessChallenge.Models.Enums;
using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.Steps;
using Multifactor.Radius.Adapter.v2.Application.Features.Radius.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.Radius.Models.Enums;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configurations.Models;

namespace Multifactor.Radius.Adapter.v2.Tests.Application.Pipeline.Steps
{
    public class AccessChallengeStepTests
    {
        private readonly Mock<IChallengeProcessorProvider> _challengeProcessorProviderMock;
        private readonly Mock<ILogger<AccessChallengeStep>> _loggerMock;
        private readonly AccessChallengeStep _accessChallengeStep;

        public AccessChallengeStepTests()
        {
            _challengeProcessorProviderMock = new Mock<IChallengeProcessorProvider>();
            _loggerMock = new Mock<ILogger<AccessChallengeStep>>();
            
            _accessChallengeStep = new AccessChallengeStep(
                _challengeProcessorProviderMock.Object,
                _loggerMock.Object
            );
        }

        [Fact]
        public async Task ExecuteAsync_WhenStateIsNullOrEmpty_ShouldReturnImmediately()
        {
            // Arrange
            var context = CreateTestContext();
            context.RequestPacket.RemoveAttribute("State");

            // Act
            await _accessChallengeStep.ExecuteAsync(context);

            // Assert
            Assert.False(context.IsTerminated);
            _challengeProcessorProviderMock.Verify(
                x => x.GetChallengeProcessorByIdentifier(It.IsAny<ChallengeIdentifier>()),
                Times.Never);
        }

        [Fact]
        public async Task ExecuteAsync_WhenProcessorNotFound_ShouldReturnImmediately()
        {
            // Arrange
            var context = CreateTestContext();
            context.RequestPacket.ReplaceAttribute("State", "test-state-123");

            _challengeProcessorProviderMock
                .Setup(x => x.GetChallengeProcessorByIdentifier(It.IsAny<ChallengeIdentifier>()))
                .Returns((IChallengeProcessor?)null);

            // Act
            await _accessChallengeStep.ExecuteAsync(context);

            // Assert
            Assert.False(context.IsTerminated);
            _challengeProcessorProviderMock.Verify(
                x => x.GetChallengeProcessorByIdentifier(It.Is<ChallengeIdentifier>(
                    id => id.RequestId == "test-state-123" && 
                          id.ToString() == "test-client-test-state-123")),
                Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_WhenProcessorReturnsAccept_ShouldNotTerminate()
        {
            // Arrange
            var context = CreateTestContext();
            context.RequestPacket.ReplaceAttribute("State", "test-state-123");

            var processorMock = new Mock<IChallengeProcessor>();
            processorMock.Setup(x => x.ProcessChallengeAsync(
                It.IsAny<ChallengeIdentifier>(),
                It.IsAny<RadiusPipelineContext>()))
                .ReturnsAsync(ChallengeStatus.Accept);

            _challengeProcessorProviderMock
                .Setup(x => x.GetChallengeProcessorByIdentifier(It.IsAny<ChallengeIdentifier>()))
                .Returns(processorMock.Object);

            // Act
            await _accessChallengeStep.ExecuteAsync(context);

            // Assert
            Assert.False(context.IsTerminated);
            processorMock.Verify(
                x => x.ProcessChallengeAsync(
                    It.Is<ChallengeIdentifier>(id => id.RequestId == "test-state-123"),
                    context),
                Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_WhenProcessorReturnsReject_ShouldTerminate()
        {
            // Arrange
            var context = CreateTestContext();
            context.RequestPacket.ReplaceAttribute("State", "test-state-123");

            var processorMock = new Mock<IChallengeProcessor>();
            processorMock.Setup(x => x.ProcessChallengeAsync(
                It.IsAny<ChallengeIdentifier>(),
                It.IsAny<RadiusPipelineContext>()))
                .ReturnsAsync(ChallengeStatus.Reject);

            _challengeProcessorProviderMock
                .Setup(x => x.GetChallengeProcessorByIdentifier(It.IsAny<ChallengeIdentifier>()))
                .Returns(processorMock.Object);

            // Act
            await _accessChallengeStep.ExecuteAsync(context);

            // Assert
            Assert.True(context.IsTerminated);
            processorMock.Verify(
                x => x.ProcessChallengeAsync(
                    It.Is<ChallengeIdentifier>(id => id.RequestId == "test-state-123"),
                    context),
                Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_WhenProcessorReturnsInProcess_ShouldTerminate()
        {
            // Arrange
            var context = CreateTestContext();
            context.RequestPacket.ReplaceAttribute("State", "test-state-123");

            var processorMock = new Mock<IChallengeProcessor>();
            processorMock.Setup(x => x.ProcessChallengeAsync(
                It.IsAny<ChallengeIdentifier>(),
                It.IsAny<RadiusPipelineContext>()))
                .ReturnsAsync(ChallengeStatus.InProcess);

            _challengeProcessorProviderMock
                .Setup(x => x.GetChallengeProcessorByIdentifier(It.IsAny<ChallengeIdentifier>()))
                .Returns(processorMock.Object);

            // Act
            await _accessChallengeStep.ExecuteAsync(context);

            // Assert
            Assert.True(context.IsTerminated);
            processorMock.Verify(
                x => x.ProcessChallengeAsync(
                    It.Is<ChallengeIdentifier>(id => id.RequestId == "test-state-123"),
                    context),
                Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_WhenProcessorReturnsUnexpectedStatus_ShouldThrow()
        {
            // Arrange
            var context = CreateTestContext();
            context.RequestPacket.ReplaceAttribute("State", "test-state-123");

            var processorMock = new Mock<IChallengeProcessor>();
            processorMock.Setup(x => x.ProcessChallengeAsync(
                It.IsAny<ChallengeIdentifier>(),
                It.IsAny<RadiusPipelineContext>()))
                .ReturnsAsync((ChallengeStatus)999); // Invalid enum value

            _challengeProcessorProviderMock
                .Setup(x => x.GetChallengeProcessorByIdentifier(It.IsAny<ChallengeIdentifier>()))
                .Returns(processorMock.Object);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _accessChallengeStep.ExecuteAsync(context));
        }

        [Fact]
        public async Task ExecuteAsync_ShouldCreateCorrectChallengeIdentifier()
        {
            // Arrange
            var context = CreateTestContext();
            context.ClientConfiguration.Name = "my-client";
            context.RequestPacket.ReplaceAttribute("State", "test-state-456");

            var capturedIdentifier = (ChallengeIdentifier?)null;
            var processorMock = new Mock<IChallengeProcessor>();
            processorMock.Setup(x => x.ProcessChallengeAsync(
                It.IsAny<ChallengeIdentifier>(),
                It.IsAny<RadiusPipelineContext>()))
                .Callback<ChallengeIdentifier, RadiusPipelineContext>((id, ctx) =>
                {
                    capturedIdentifier = id;
                })
                .ReturnsAsync(ChallengeStatus.Accept);

            _challengeProcessorProviderMock
                .Setup(x => x.GetChallengeProcessorByIdentifier(It.IsAny<ChallengeIdentifier>()))
                .Returns(processorMock.Object);

            // Act
            await _accessChallengeStep.ExecuteAsync(context);

            // Assert
            Assert.NotNull(capturedIdentifier);
            Assert.Equal("my-client", capturedIdentifier.ToString().Split('-')[0]);
            Assert.Equal("challenge-state-456", capturedIdentifier.RequestId);
            Assert.Equal("my-client-challenge-state-456", capturedIdentifier.ToString());
        }

        [Fact]
        public async Task ExecuteAsync_ShouldLogDebugMessage()
        {
            // Arrange
            var context = CreateTestContext();
            context.RequestPacket.ReplaceAttribute("State", "test-state");

            var processorMock = new Mock<IChallengeProcessor>();
            processorMock.Setup(x => x.ProcessChallengeAsync(
                It.IsAny<ChallengeIdentifier>(),
                It.IsAny<RadiusPipelineContext>()))
                .ReturnsAsync(ChallengeStatus.Accept);

            _challengeProcessorProviderMock
                .Setup(x => x.GetChallengeProcessorByIdentifier(It.IsAny<ChallengeIdentifier>()))
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
                        logMessages.Add(formatter(state, exception));
                    });

            // Act
            await _accessChallengeStep.ExecuteAsync(context);

            // Assert
            Assert.Contains(logMessages, msg => msg.Contains(nameof(AccessChallengeStep)));
        }

        private static RadiusPipelineContext CreateTestContext()
        {
            var clientConfig = new ClientConfiguration
            {
                Name = "test-client",
                RadiusSharedSecret = "test-secret"
            };

            var requestPacket = new RadiusPacket(
                new RadiusPacketHeader(PacketCode.AccessRequest, 1, new byte[16]));
            
            requestPacket.AddAttributeValue("State", "initial-state");

            return new RadiusPipelineContext(requestPacket, clientConfig);
        }
    }
}