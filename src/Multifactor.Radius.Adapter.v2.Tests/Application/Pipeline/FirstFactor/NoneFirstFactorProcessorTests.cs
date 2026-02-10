using Microsoft.Extensions.Logging;
using Moq;
using Multifactor.Radius.Adapter.v2.Application.Configuration.Models.Enum;
using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.FirstFactor;
using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.Models.Enum;
using Multifactor.Radius.Adapter.v2.Application.Features.Radius.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.Radius.Models.Enums;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configurations.Models;

namespace Multifactor.Radius.Adapter.v2.Tests.Application.Pipeline.FirstFactor
{
    public class NoneFirstFactorProcessorTests
    {
        private readonly Mock<ILogger<NoneFirstFactorProcessor>> _loggerMock;
        private readonly NoneFirstFactorProcessor _processor;

        public NoneFirstFactorProcessorTests()
        {
            _loggerMock = new Mock<ILogger<NoneFirstFactorProcessor>>();
            _processor = new NoneFirstFactorProcessor(_loggerMock.Object);
        }

        [Fact]
        public void AuthenticationSource_ShouldBeNone()
        {
            // Act & Assert
            Assert.Equal(AuthenticationSource.None, _processor.AuthenticationSource);
        }

        [Fact]
        public async Task ProcessFirstFactor_ShouldAlwaysAccept()
        {
            // Arrange
            var requestPacket = CreateRadiusPacket();
            var context = CreateContext(requestPacket);

            // Act
            await _processor.ProcessFirstFactor(context);

            // Assert
            Assert.Equal(AuthenticationStatus.Accept, context.FirstFactorStatus);
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Bypass first factor")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task ProcessFirstFactor_ShouldAcceptEvenWithEmptyUserName()
        {
            // Arrange
            var requestPacket = CreateRadiusPacket(userName: null);
            var context = CreateContext(requestPacket);

            // Act
            await _processor.ProcessFirstFactor(context);

            // Assert
            Assert.Equal(AuthenticationStatus.Accept, context.FirstFactorStatus);
        }

        private RadiusPacket CreateRadiusPacket(string userName = "testuser")
        {
            var header = new RadiusPacketHeader(PacketCode.AccessRequest, 1, new byte[16]);
            var packet = new RadiusPacket(header);
            
            if (userName != null)
            {
                packet.AddAttributeValue("User-Name", userName);
            }
            
            return packet;
        }

        private RadiusPipelineContext CreateContext(RadiusPacket requestPacket)
        {
            var clientConfig = new ClientConfiguration
            {
                Name = "TestClient",
                FirstFactorAuthenticationSource = AuthenticationSource.None
            };
            
            return new RadiusPipelineContext(requestPacket, clientConfig);
        }
    }
}