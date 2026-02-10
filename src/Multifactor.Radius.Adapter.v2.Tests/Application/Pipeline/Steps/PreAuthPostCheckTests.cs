using Microsoft.Extensions.Logging;
using Moq;
using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.Models.Enum;
using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.Steps;
using Multifactor.Radius.Adapter.v2.Application.Features.Radius.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.Radius.Models.Enums;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configurations.Models;

namespace Multifactor.Radius.Adapter.v2.Tests.Application.Pipeline.Steps
{
    public class PreAuthPostCheckTests
    {
        private readonly Mock<ILogger<PreAuthPostCheck>> _loggerMock;
        private readonly PreAuthPostCheck _step;

        public PreAuthPostCheckTests()
        {
            _loggerMock = new Mock<ILogger<PreAuthPostCheck>>();
            _step = new PreAuthPostCheck(_loggerMock.Object);
        }

        [Fact]
        public async Task ExecuteAsync_WhenSecondFactorAccept_DoesNotTerminate()
        {
            // Arrange
            var context = CreateTestContext();
            context.SecondFactorStatus = AuthenticationStatus.Accept;

            // Act
            await _step.ExecuteAsync(context);

            // Assert
            Assert.False(context.IsTerminated);
        }

        [Fact]
        public async Task ExecuteAsync_WhenSecondFactorBypass_DoesNotTerminate()
        {
            // Arrange
            var context = CreateTestContext();
            context.SecondFactorStatus = AuthenticationStatus.Bypass;

            // Act
            await _step.ExecuteAsync(context);

            // Assert
            Assert.False(context.IsTerminated);
        }

        [Fact]
        public async Task ExecuteAsync_WhenSecondFactorReject_Terminates()
        {
            // Arrange
            var context = CreateTestContext();
            context.SecondFactorStatus = AuthenticationStatus.Reject;

            // Act
            await _step.ExecuteAsync(context);

            // Assert
            Assert.True(context.IsTerminated);
        }

        [Fact]
        public async Task ExecuteAsync_WhenSecondFactorAwaiting_Terminates()
        {
            // Arrange
            var context = CreateTestContext();
            context.SecondFactorStatus = AuthenticationStatus.Awaiting;

            // Act
            await _step.ExecuteAsync(context);

            // Assert
            Assert.True(context.IsTerminated);
        }

        [Fact]
        public async Task ExecuteAsync_WhenUserNameNull_LogsWithNull()
        {
            // Arrange
            var context = CreateTestContext();
            context.RequestPacket.RemoveAttribute("User-Name");
            context.SecondFactorStatus = AuthenticationStatus.Accept;

            var logMessages = new List<string>();
            SetupLogCapture(logMessages);

            // Act
            await _step.ExecuteAsync(context);

            // Assert
            Assert.Contains(logMessages, msg => 
                msg.Contains("continued pipeline") && 
                msg.Contains("''"));
        }

        [Fact]
        public async Task ExecuteAsync_WhenLdapSchemaNull_LogsWithoutDomain()
        {
            // Arrange
            var context = CreateTestContext();
            context.LdapSchema = null;
            context.SecondFactorStatus = AuthenticationStatus.Reject;

            var logMessages = new List<string>();
            SetupLogCapture(logMessages);

            // Act
            await _step.ExecuteAsync(context);

            // Assert
            Assert.Contains(logMessages, msg => 
                msg.Contains("terminated pipeline") && 
                !msg.Contains("at '"));
        }

        [Fact]
        public async Task ExecuteAsync_LogsDebugOnStart()
        {
            // Arrange
            var context = CreateTestContext();
            context.SecondFactorStatus = AuthenticationStatus.Accept;

            var logMessages = new List<string>();
            SetupLogCapture(logMessages);

            // Act
            await _step.ExecuteAsync(context);

            // Assert
            Assert.Contains(logMessages, msg => 
                msg.Contains(nameof(PreAuthPostCheck)) && 
                msg.Contains("started"));
        }

        [Fact]
        public async Task ExecuteAsync_WhenAlreadyTerminated_StillLogs()
        {
            // Arrange
            var context = CreateTestContext();
            context.SecondFactorStatus = AuthenticationStatus.Reject;
            context.Terminate(); // Already terminated

            var logMessages = new List<string>();
            SetupLogCapture(logMessages);

            // Act
            await _step.ExecuteAsync(context);

            // Assert
            Assert.Contains(logMessages, msg => msg.Contains("terminated pipeline"));
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
                        if (level == LogLevel.Debug)
                            logMessages.Add(formatter(state, exception));
                    });
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
            requestPacket.AddAttributeValue("User-Name", "testuser");

            return new RadiusPipelineContext(requestPacket, clientConfig);
        }
    }
}