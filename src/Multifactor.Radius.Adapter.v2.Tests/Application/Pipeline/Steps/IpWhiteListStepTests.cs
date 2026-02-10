using System.Net;
using Microsoft.Extensions.Logging;
using Moq;
using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.Models.Enum;
using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.Steps;
using Multifactor.Radius.Adapter.v2.Application.Features.Radius.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.Radius.Models.Enums;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configurations.Models;
using NetTools;

namespace Multifactor.Radius.Adapter.v2.Tests.Application.Pipeline.Steps
{
    public class IpWhiteListStepTests
    {
        private readonly Mock<ILogger<IpWhiteListStep>> _loggerMock;
        private readonly IpWhiteListStep _step;

        public IpWhiteListStepTests()
        {
            _loggerMock = new Mock<ILogger<IpWhiteListStep>>();
            _step = new IpWhiteListStep(_loggerMock.Object);
        }

        [Fact]
        public async Task ExecuteAsync_ShouldAllowWhenIpInWhiteList()
        {
            // Arrange
            var whiteList = new List<IPAddressRange>
            {
                IPAddressRange.Parse("192.168.1.0/24"),
                IPAddressRange.Parse("10.0.0.0/8")
            };

            var clientConfig = new ClientConfiguration
            {
                Name = "TestClient",
                IpWhiteList = whiteList
            };

            var requestPacket = CreateAccessRequestPacket("192.168.1.100");
            var context = new RadiusPipelineContext(requestPacket, clientConfig);

            // Act
            await _step.ExecuteAsync(context);

            // Assert
            Assert.False(context.IsTerminated);
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Debug,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("is in the allowed IP range")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_ShouldTerminateWhenIpNotInWhiteList()
        {
            // Arrange
            var whiteList = new List<IPAddressRange>
            {
                IPAddressRange.Parse("192.168.1.0/24")
            };

            var clientConfig = new ClientConfiguration
            {
                Name = "TestClient",
                IpWhiteList = whiteList
            };

            var requestPacket = CreateAccessRequestPacket("10.0.0.100"); // Not in whitelist
            var context = new RadiusPipelineContext(requestPacket, clientConfig);

            // Act
            await _step.ExecuteAsync(context);

            // Assert
            Assert.True(context.IsTerminated);
            Assert.Equal(AuthenticationStatus.Reject, context.FirstFactorStatus);
            Assert.Equal(AuthenticationStatus.Reject, context.SecondFactorStatus);
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Debug,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("is not in the allowed IP range")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_ShouldSkipWhenWhiteListEmpty()
        {
            // Arrange
            var clientConfig = new ClientConfiguration
            {
                Name = "TestClient",
                IpWhiteList = new List<IPAddressRange>()
            };

            var requestPacket = CreateAccessRequestPacket("192.168.1.100");
            var context = new RadiusPipelineContext(requestPacket, clientConfig);

            // Act
            await _step.ExecuteAsync(context);

            // Assert
            Assert.False(context.IsTerminated);
        }

        [Fact]
        public async Task ExecuteAsync_ShouldUseCallingStationIdWhenAvailable()
        {
            // Arrange
            var whiteList = new List<IPAddressRange>
            {
                IPAddressRange.Parse("192.168.1.26-192.168.1.32"),
            };

            var clientConfig = new ClientConfiguration
            {
                Name = "TestClient",
                IpWhiteList = whiteList
            };

            var requestPacket = CreateAccessRequestPacket("192.168.1.100");
            requestPacket.AddAttributeValue("Calling-Station-Id", "192.168.1.50"); // Different IP
            var context = new RadiusPipelineContext(requestPacket, clientConfig);

            // Act
            await _step.ExecuteAsync(context);

            // Assert
            Assert.True(context.IsTerminated); // Should reject because 192.168.1.50 is not in whitelist
        }

        private RadiusPacket CreateAccessRequestPacket(string remoteIp)
        {
            var header = new RadiusPacketHeader(PacketCode.AccessRequest, 1, new byte[16]);
            var packet = new RadiusPacket(header)
            {
                RemoteEndpoint = new IPEndPoint(IPAddress.Parse(remoteIp), 1812)
            };
            packet.AddAttributeValue("User-Name", "testuser");
            return packet;
        }
    }
}