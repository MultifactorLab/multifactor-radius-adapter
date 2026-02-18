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
    public class StatusServerFilteringStepTests
    {
        private readonly ApplicationVariables _appVars;
        private readonly Mock<ILogger<StatusServerFilteringStep>> _loggerMock;
        private readonly StatusServerFilteringStep _step;

        public StatusServerFilteringStepTests()
        {
            _appVars = new ApplicationVariables
            {
                AppVersion = "1.2.3",
                StartedAt = DateTime.Now.AddDays(-1).AddHours(-2).AddMinutes(-30) // 1 day, 2 hours, 30 minutes ago
            };
            
            _loggerMock = new Mock<ILogger<StatusServerFilteringStep>>();
            _step = new StatusServerFilteringStep(_appVars, _loggerMock.Object);
        }

        [Fact]
        public async Task ExecuteAsync_WhenStatusServer_SetsResponseAndTerminates()
        {
            // Arrange
            var context = CreateTestContext(PacketCode.StatusServer);

            // Act
            await _step.ExecuteAsync(context);

            // Assert
            Assert.True(context.IsTerminated);
            Assert.Equal(AuthenticationStatus.Accept, context.FirstFactorStatus);
            Assert.Equal(AuthenticationStatus.Accept, context.SecondFactorStatus);
            
            Assert.Contains("Server up", context.ResponseInformation.ReplyMessage);
            Assert.Contains("1 days", context.ResponseInformation.ReplyMessage);
            Assert.Contains("02:30", context.ResponseInformation.ReplyMessage); // Hours and minutes
            Assert.Contains("ver.: 1.2.3", context.ResponseInformation.ReplyMessage);
        }

        [Fact]
        public async Task ExecuteAsync_WhenNotStatusServer_DoesNothing()
        {
            // Arrange
            var context = CreateTestContext(PacketCode.AccessRequest);

            // Act
            await _step.ExecuteAsync(context);

            // Assert
            Assert.False(context.IsTerminated);
            Assert.Equal(AuthenticationStatus.Awaiting, context.FirstFactorStatus);
            Assert.Equal(AuthenticationStatus.Awaiting, context.SecondFactorStatus);
            Assert.Null(context.ResponseInformation.ReplyMessage);
        }

        [Theory]
        [InlineData(PacketCode.AccessRequest)]
        [InlineData(PacketCode.AccessAccept)]
        [InlineData(PacketCode.AccessReject)]
        [InlineData(PacketCode.AccessChallenge)]
        [InlineData(PacketCode.AccountingRequest)]
        [InlineData(PacketCode.AccountingResponse)]
        [InlineData(PacketCode.StatusClient)]
        [InlineData(PacketCode.DisconnectRequest)]
        [InlineData(PacketCode.DisconnectAck)]
        [InlineData(PacketCode.DisconnectNak)]
        [InlineData(PacketCode.CoaRequest)]
        [InlineData(PacketCode.CoaAck)]
        [InlineData(PacketCode.CoaNak)]
        public async Task ExecuteAsync_ForNonStatusServerPackets_DoesNotTerminate(PacketCode packetCode)
        {
            // Arrange
            var context = CreateTestContext(packetCode);

            // Act
            await _step.ExecuteAsync(context);

            // Assert
            Assert.False(context.IsTerminated);
            Assert.Equal(AuthenticationStatus.Awaiting, context.FirstFactorStatus);
            Assert.Equal(AuthenticationStatus.Awaiting, context.SecondFactorStatus);
        }

        [Fact]
        public async Task ExecuteAsync_WhenAppVersionNull_StillWorks()
        {
            // Arrange
            var appVars = new ApplicationVariables
            {
                AppVersion = null,
                StartedAt = DateTime.Now.AddHours(-5)
            };
            
            var step = new StatusServerFilteringStep(appVars, _loggerMock.Object);
            var context = CreateTestContext(PacketCode.StatusServer);

            // Act
            await step.ExecuteAsync(context);

            // Assert
            Assert.True(context.IsTerminated);
            Assert.Contains("Server up", context.ResponseInformation.ReplyMessage);
            Assert.Contains("ver.:", context.ResponseInformation.ReplyMessage);
        }

        [Fact]
        public async Task ExecuteAsync_WhenUptimeZeroDays_ShowsCorrectFormat()
        {
            // Arrange
            var appVars = new ApplicationVariables
            {
                AppVersion = "1.0",
                StartedAt = DateTime.Now.AddHours(-3).AddMinutes(-15) // 3 hours, 15 minutes ago
            };
            
            var step = new StatusServerFilteringStep(appVars, _loggerMock.Object);
            var context = CreateTestContext(PacketCode.StatusServer);

            // Act
            await step.ExecuteAsync(context);

            // Assert
            Assert.Contains("0 days", context.ResponseInformation.ReplyMessage);
            Assert.Contains("03:15", context.ResponseInformation.ReplyMessage);
        }

        [Fact]
        public async Task ExecuteAsync_LogsDebugOnStart()
        {
            // Arrange
            var context = CreateTestContext(PacketCode.StatusServer);
            var logMessages = new List<string>();
            SetupLogCapture(logMessages);

            // Act
            await _step.ExecuteAsync(context);

            // Assert
            Assert.Contains(logMessages, msg => 
                msg.Contains(nameof(StatusServerFilteringStep)) && 
                msg.Contains("started"));
        }

        [Fact]
        public async Task ExecuteAsync_ReturnsCompletedTask()
        {
            // Arrange
            var context = CreateTestContext(PacketCode.AccessRequest);

            // Act
            var task = _step.ExecuteAsync(context);

            // Assert
            Assert.True(task.IsCompleted);
            await task;
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

        private static RadiusPipelineContext CreateTestContext(PacketCode packetCode)
        {
            var clientConfig = new ClientConfiguration
            {
                Name = "test-client",
                RadiusSharedSecret = "test-secret"
            };

            var requestPacket = new RadiusPacket(
                new RadiusPacketHeader(packetCode, 1, new byte[16]));

            return new RadiusPipelineContext(requestPacket, clientConfig);
        }
    }
}