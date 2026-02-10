using System.Net;
using Microsoft.Extensions.Logging;
using Moq;
using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.Steps;
using Multifactor.Radius.Adapter.v2.Application.Features.Radius.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.Radius.Models.Enums;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configurations.Models;

namespace Multifactor.Radius.Adapter.v2.Tests.Application.Pipeline.Steps
{
    public class AccessRequestFilteringStepTests
    {
        private readonly Mock<ILogger<AccessRequestFilteringStep>> _loggerMock;
        private readonly AccessRequestFilteringStep _step;

        public AccessRequestFilteringStepTests()
        {
            _loggerMock = new Mock<ILogger<AccessRequestFilteringStep>>();
            _step = new AccessRequestFilteringStep(_loggerMock.Object);
        }

        [Fact]
        public async Task ExecuteAsync_ShouldAllowAccessRequest()
        {
            // Arrange
            var requestPacket = CreateRadiusPacket(PacketCode.AccessRequest);
            var context = CreateContext(requestPacket);

            // Act
            await _step.ExecuteAsync(context);

            // Assert
            Assert.False(context.IsTerminated);
            Assert.False(context.ShouldSkipResponse);
        }

        [Theory]
        [InlineData(PacketCode.AccountingRequest)]
        [InlineData(PacketCode.AccountingResponse)]
        [InlineData(PacketCode.StatusClient)]
        [InlineData(PacketCode.DisconnectRequest)]
        [InlineData(PacketCode.DisconnectAck)]
        [InlineData(PacketCode.DisconnectNak)]
        [InlineData(PacketCode.CoaRequest)]
        [InlineData(PacketCode.CoaAck)]
        [InlineData(PacketCode.CoaNak)]
        public async Task ExecuteAsync_ShouldTerminateNonAccessRequests(PacketCode packetCode)
        {
            // Arrange
            var requestPacket = CreateRadiusPacket(packetCode);
            var context = CreateContext(requestPacket);

            // Act
            await _step.ExecuteAsync(context);

            // Assert
            Assert.True(context.IsTerminated);
            Assert.True(context.ShouldSkipResponse);
            
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Unprocessable packet type")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_ShouldLogClientInfoWhenTerminating()
        {
            // Arrange
            var requestPacket = CreateRadiusPacket(PacketCode.AccountingRequest);
            var context = CreateContext(requestPacket);

            // Act
            await _step.ExecuteAsync(context);

            // Assert
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("192.168.1.100")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_ShouldHandleNullProxyEndpoint()
        {
            // Arrange
            var requestPacket = CreateRadiusPacket(PacketCode.AccountingRequest);
            requestPacket.ProxyEndpoint = null;
            var context = CreateContext(requestPacket);

            // Act
            await _step.ExecuteAsync(context);

            // Assert
            Assert.True(context.IsTerminated);
            // Не должно быть исключения
        }

        private RadiusPacket CreateRadiusPacket(PacketCode code)
        {
            var header = new RadiusPacketHeader(code, 1, new byte[16]);
            var packet = new RadiusPacket(header)
            {
                RemoteEndpoint = new IPEndPoint(IPAddress.Parse("192.168.1.100"), 1812)
            };
            
            return packet;
        }

        private RadiusPipelineContext CreateContext(RadiusPacket requestPacket)
        {
            var clientConfig = new ClientConfiguration { Name = "TestClient" };
            return new RadiusPipelineContext(requestPacket, clientConfig);
        }
    }
}