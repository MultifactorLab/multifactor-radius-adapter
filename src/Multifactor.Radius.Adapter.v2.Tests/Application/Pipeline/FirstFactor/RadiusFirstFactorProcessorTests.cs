using System.Net;
using Microsoft.Extensions.Logging;
using Moq;
using Multifactor.Radius.Adapter.v2.Application.Configuration.Models.Enum;
using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.FirstFactor;
using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.Models.Enum;
using Multifactor.Radius.Adapter.v2.Application.Features.Radius.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.Radius.Models.Enums;
using Multifactor.Radius.Adapter.v2.Application.Features.Radius.Ports;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configurations.Models;

namespace Multifactor.Radius.Adapter.v2.Tests.Application.Pipeline.FirstFactor
{
    public class RadiusFirstFactorProcessorTests
    {
        private readonly Mock<IRadiusPacketService> _radiusPacketServiceMock;
        private readonly Mock<ILogger<RadiusFirstFactorProcessor>> _loggerMock;
        private readonly Mock<IRadiusClient> _radiusClientMock;
        private readonly RadiusFirstFactorProcessor _processor;

        public RadiusFirstFactorProcessorTests()
        {
            _radiusPacketServiceMock = new Mock<IRadiusPacketService>();
            var radiusClientFactoryMock = new Mock<IRadiusClientFactory>();
            _loggerMock = new Mock<ILogger<RadiusFirstFactorProcessor>>();
            _radiusClientMock = new Mock<IRadiusClient>();
            
            _processor = new RadiusFirstFactorProcessor(
                _radiusPacketServiceMock.Object,
                radiusClientFactoryMock.Object,
                _loggerMock.Object);

            radiusClientFactoryMock
                .Setup(x => x.CreateRadiusClient(It.IsAny<IPEndPoint>()))
                .Returns(_radiusClientMock.Object);
        }

        [Fact]
        public void AuthenticationSource_ShouldBeRadius()
        {
            // Act & Assert
            Assert.Equal(AuthenticationSource.Radius, _processor.AuthenticationSource);
        }

        [Fact]
        public async Task ProcessFirstFactor_ShouldRejectWhenUserNameMissing()
        {
            // Arrange
            var clientConfiguration = CreateTestClientConfiguration();
            var requestPacket = CreateRadiusPacket(userName: null);
            var context = CreateContext(requestPacket, clientConfiguration);

            // Act
            await _processor.ProcessFirstFactor(context);

            // Assert
            Assert.Equal(AuthenticationStatus.Reject, context.FirstFactorStatus);
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Can't find User-Name")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task ProcessFirstFactor_ShouldAcceptWhenRadiusAccepts()
        {
            // Arrange
            var requestPacket = CreateRadiusPacket("testuser");
            var clientConfiguration = CreateTestClientConfiguration();
            var context = CreateContext(requestPacket, clientConfiguration);
            
            var responsePacket = new RadiusPacket(
                new RadiusPacketHeader(PacketCode.AccessAccept, 1, new byte[16]));
            
            SetupSuccessfulRadiusCall(responsePacket);

            // Act
            await _processor.ProcessFirstFactor(context);

            // Assert
            Assert.Equal(AuthenticationStatus.Accept, context.FirstFactorStatus);
            Assert.Equal(responsePacket, context.ResponsePacket);
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("verified successfully")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task ProcessFirstFactor_ShouldRejectWhenRadiusRejects()
        {
            // Arrange
            var requestPacket = CreateRadiusPacket("testuser");
            var clientConfiguration = CreateTestClientConfiguration();
            var context = CreateContext(requestPacket, clientConfiguration);
            
            var responsePacket = new RadiusPacket(
                new RadiusPacketHeader(PacketCode.AccessReject, 1, new byte[16]));
            
            SetupSuccessfulRadiusCall(responsePacket);

            // Act
            await _processor.ProcessFirstFactor(context);

            // Assert
            Assert.Equal(AuthenticationStatus.Reject, context.FirstFactorStatus);
        }

        [Fact]
        public async Task ProcessFirstFactor_ShouldTryNextServerWhenFirstFails()
        {
            // Arrange
            var npsServerEndpoints = new[]
            {
                new IPEndPoint(IPAddress.Parse("192.168.1.1"), 1812),
                new IPEndPoint(IPAddress.Parse("192.168.1.2"), 1812)
            };
            var requestPacket = CreateRadiusPacket("testuser");
            var clientConfiguration = CreateTestClientConfiguration(npsServerEndpoints);
            var context = CreateContext(requestPacket, clientConfiguration);
            

            
            var responsePacket = new RadiusPacket(
                new RadiusPacketHeader(PacketCode.AccessAccept, 1, new byte[16]));
            
            // Первый сервер не отвечает, второй отвечает успешно
            _radiusClientMock
                .SetupSequence(x => x.SendPacketAsync(
                    It.IsAny<byte>(),
                    It.IsAny<byte[]>(),
                    It.IsAny<IPEndPoint>(),
                    It.IsAny<TimeSpan>()))
                .ReturnsAsync((byte[])null)
                .ReturnsAsync(new byte[100]);
            
            _radiusPacketServiceMock
                .Setup(x => x.SerializePacket(It.IsAny<RadiusPacket>(), It.IsAny<SharedSecret>()))
                .Returns(new byte[100]);
            
            _radiusPacketServiceMock
                .Setup(x => x.ParsePacket(It.IsAny<byte[]>(), It.IsAny<SharedSecret>(), It.IsAny<RadiusAuthenticator>()))
                .Returns(responsePacket);

            // Act
            await _processor.ProcessFirstFactor(context);

            // Assert
            Assert.Equal(AuthenticationStatus.Accept, context.FirstFactorStatus);
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("did not respond")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task ProcessFirstFactor_ShouldRejectWhenAllServersFail()
        {
            // Arrange
            var npsServerEndpoints = new[]
            {
                new IPEndPoint(IPAddress.Parse("192.168.1.1"), 1812),
                new IPEndPoint(IPAddress.Parse("192.168.1.2"), 1812)
            };
            var requestPacket = CreateRadiusPacket("testuser");
            var clientConfiguration = CreateTestClientConfiguration(npsServerEndpoints);
            var context = CreateContext(requestPacket, clientConfiguration);
            
            _radiusClientMock
                .Setup(x => x.SendPacketAsync(
                    It.IsAny<byte>(),
                    It.IsAny<byte[]>(),
                    It.IsAny<IPEndPoint>(),
                    It.IsAny<TimeSpan>()))
                .ReturnsAsync((byte[])null);

            // Act
            await _processor.ProcessFirstFactor(context);

            // Assert
            Assert.Equal(AuthenticationStatus.Reject, context.FirstFactorStatus);
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("did not respond")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Exactly(2));
        }

        private void SetupSuccessfulRadiusCall(RadiusPacket responsePacket)
        {
            _radiusClientMock
                .Setup(x => x.SendPacketAsync(
                    It.IsAny<byte>(),
                    It.IsAny<byte[]>(),
                    It.IsAny<IPEndPoint>(),
                    It.IsAny<TimeSpan>()))
                .ReturnsAsync(new byte[100]);
            
            _radiusPacketServiceMock
                .Setup(x => x.SerializePacket(It.IsAny<RadiusPacket>(), It.IsAny<SharedSecret>()))
                .Returns(new byte[100]);
            
            _radiusPacketServiceMock
                .Setup(x => x.ParsePacket(It.IsAny<byte[]>(), It.IsAny<SharedSecret>(), It.IsAny<RadiusAuthenticator>()))
                .Returns(responsePacket);
        }

        private RadiusPacket CreateRadiusPacket(string userName)
        {
            var header = new RadiusPacketHeader(PacketCode.AccessRequest, 1, new byte[16]);
            var packet = new RadiusPacket(header)
            {
                RemoteEndpoint = new  IPEndPoint(IPAddress.Any, 228),
            };
            
            if (userName != null)
            {
                packet.AddAttributeValue("User-Name", userName);
                packet.AddAttributeValue("User-Password", "password");
            }
            
            return packet;
        }

        private static ClientConfiguration CreateTestClientConfiguration(IPEndPoint[]? npsServerEndpoints = null)
        {
            return new ClientConfiguration
            {
                Name = "TestClient",
                RadiusSharedSecret = "shared-secret",
                AdapterClientEndpoint = new IPEndPoint(IPAddress.Any, 0),
                NpsServerEndpoints = npsServerEndpoints ?? [new IPEndPoint(IPAddress.Parse("192.168.1.1"), 1812)],
                NpsServerTimeout = TimeSpan.FromSeconds(5)
            };
        }

        private static RadiusPipelineContext CreateContext(RadiusPacket requestPacket, ClientConfiguration clientConfig)
        {

            
            var context = new RadiusPipelineContext(requestPacket, clientConfig)
            {
                Passphrase = UserPassphrase.Parse("password", PreAuthMode.None)
            };
            
            return context;
        }
    }
}