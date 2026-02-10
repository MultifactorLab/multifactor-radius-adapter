using Microsoft.Extensions.Logging;
using Moq;
using Multifactor.Radius.Adapter.v2.Application.Core.Models.Enum;
using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.Interfaces;
using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.Radius.Exceptions;
using Multifactor.Radius.Adapter.v2.Application.Features.Radius.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.Radius.Models.Enums;
using Multifactor.Radius.Adapter.v2.Application.Features.Radius.Ports;
using Multifactor.Radius.Adapter.v2.Application.Features.Radius.Services;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configurations.Models;

namespace Multifactor.Radius.Adapter.v2.Tests.Application.Radius
{
    public class RadiusPacketProcessorTests
    {
        private readonly Mock<IPipelineProvider> _pipelineProviderMock;
        private readonly Mock<IResponseSender> _responseSenderMock;
        private readonly Mock<ILogger<RadiusPacketProcessor>> _loggerMock;
        private readonly RadiusPacketProcessor _processor;

        public RadiusPacketProcessorTests()
        {
            _pipelineProviderMock = new Mock<IPipelineProvider>();
            _responseSenderMock = new Mock<IResponseSender>();
            _loggerMock = new Mock<ILogger<RadiusPacketProcessor>>();
            _processor = new RadiusPacketProcessor(
                _pipelineProviderMock.Object,
                _responseSenderMock.Object,
                _loggerMock.Object);
        }

        [Fact]
        public void Constructor_ShouldThrowArgumentNullException_WhenPipelineProviderIsNull()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new RadiusPacketProcessor(null, _responseSenderMock.Object, _loggerMock.Object));
        }

        [Fact]
        public void Constructor_ShouldThrowArgumentNullException_WhenResponseSenderIsNull()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new RadiusPacketProcessor(_pipelineProviderMock.Object, null, _loggerMock.Object));
        }

        [Fact]
        public void Constructor_ShouldThrowArgumentNullException_WhenLoggerIsNull()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new RadiusPacketProcessor(_pipelineProviderMock.Object, _responseSenderMock.Object, null));
        }

        [Fact]
        public async Task ProcessPacketAsync_ShouldThrowArgumentNullException_WhenRequestPacketIsNull()
        {
            // Arrange
            var clientConfig = new ClientConfiguration();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                _processor.ProcessPacketAsync(null, clientConfig));
        }

        [Fact]
        public async Task ProcessPacketAsync_ShouldThrowArgumentNullException_WhenClientConfigurationIsNull()
        {
            // Arrange
            var header = new RadiusPacketHeader();
            var packet = new RadiusPacket(header);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                _processor.ProcessPacketAsync(packet, null));
        }

        [Fact]
        public async Task ProcessPacketAsync_ShouldExecutePipelineWithoutLdap_WhenNoLdapServers()
        {
            var header = new RadiusPacketHeader();
            var packet = new RadiusPacket(header);
            var clientConfig = new ClientConfiguration
            {
                Name = "TestClient",
                LdapServers = new List<LdapServerConfiguration>()
            };

            var pipelineMock = new Mock<IRadiusPipeline>();
            _pipelineProviderMock.Setup(x => x.GetPipeline(clientConfig))
                .Returns(pipelineMock.Object);

            await _processor.ProcessPacketAsync(packet, clientConfig);

            pipelineMock.Verify(x => x.ExecuteAsync(It.IsAny<RadiusPipelineContext>()), Times.Once);
            _responseSenderMock.Verify(x => x.SendResponse(It.IsAny<SendAdapterResponseRequest>()), Times.Once);
        }

        [Fact]
        public async Task ProcessPacketAsync_ShouldExecutePipelineWithoutLdap_WhenNotAccessRequest()
        {
            // Arrange
            var packet = new Mock<RadiusPacket>();
            packet.Setup(x => x.Code).Returns(PacketCode.AccountingRequest); // Not AccessRequest
            var clientConfig = new ClientConfiguration
            {
                Name = "TestClient",
                LdapServers = new List<LdapServerConfiguration>
                {
                    new LdapServerConfiguration { ConnectionString = "ldap://server1" }
                }
            };

            var pipelineMock = new Mock<IRadiusPipeline>();
            _pipelineProviderMock.Setup(x => x.GetPipeline(clientConfig))
                .Returns(pipelineMock.Object);

            // Act
            await _processor.ProcessPacketAsync(packet.Object, clientConfig);

            // Assert
            pipelineMock.Verify(x => x.ExecuteAsync(It.IsAny<RadiusPipelineContext>()), Times.Once);
        }

        [Fact]
        public async Task ProcessPacketAsync_ShouldTryAllLdapServers_WhenFirstFails()
        {
            // Arrange
            var header = new RadiusPacketHeader();
            var packet = new RadiusPacket(header);
            var clientConfig = new ClientConfiguration
            {
                Name = "TestClient",
                LdapServers = new List<LdapServerConfiguration>
                {
                    new LdapServerConfiguration { ConnectionString = "ldap://server1" },
                    new LdapServerConfiguration { ConnectionString = "ldap://server2" }
                }
            };

            var pipelineMock = new Mock<IRadiusPipeline>();
            _pipelineProviderMock.Setup(x => x.GetPipeline(clientConfig))
                .Returns(pipelineMock.Object);

            // First execution throws
            var callCount = 0;
            pipelineMock.Setup(x => x.ExecuteAsync(It.IsAny<RadiusPipelineContext>()))
                .Callback(() =>
                {
                    callCount++;
                    if (callCount == 1)
                        throw new Exception("First server failed");
                })
                .Returns(Task.CompletedTask);

            // Act
            await _processor.ProcessPacketAsync(packet, clientConfig);

            // Assert
            pipelineMock.Verify(x => x.ExecuteAsync(It.IsAny<RadiusPipelineContext>()), Times.Exactly(2));
            _responseSenderMock.Verify(x => x.SendResponse(It.IsAny<SendAdapterResponseRequest>()), Times.Once);
        }

        [Fact]
        public async Task ProcessPacketAsync_ShouldThrowException_WhenAllLdapServersFail()
        {
            // Arrange
            var header = new RadiusPacketHeader();
            var packet = new RadiusPacket(header);
            var clientConfig = new ClientConfiguration
            {
                Name = "TestClient",
                LdapServers = new List<LdapServerConfiguration>
                {
                    new LdapServerConfiguration { ConnectionString = "ldap://server1" },
                    new LdapServerConfiguration { ConnectionString = "ldap://server2" }
                }
            };

            var pipelineMock = new Mock<IRadiusPipeline>();
            _pipelineProviderMock.Setup(x => x.GetPipeline(clientConfig))
                .Returns(pipelineMock.Object);

            pipelineMock.Setup(x => x.ExecuteAsync(It.IsAny<RadiusPipelineContext>()))
                .ThrowsAsync(new Exception("LDAP server failed"));

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() =>
                _processor.ProcessPacketAsync(packet, clientConfig));
        }

        [Fact]
        public async Task ProcessPacketAsync_ShouldStopTryingServers_WhenOneSucceeds()
        {
            // Arrange
            var header = new RadiusPacketHeader();
            var packet = new RadiusPacket(header);
            var clientConfig = new ClientConfiguration
            {
                Name = "TestClient",
                LdapServers = new List<LdapServerConfiguration>
                {
                    new LdapServerConfiguration { ConnectionString = "ldap://server1" },
                    new LdapServerConfiguration { ConnectionString = "ldap://server2" },
                    new LdapServerConfiguration { ConnectionString = "ldap://server3" }
                }
            };

            var pipelineMock = new Mock<IRadiusPipeline>();
            _pipelineProviderMock.Setup(x => x.GetPipeline(clientConfig))
                .Returns(pipelineMock.Object);

            var callCount = 0;
            pipelineMock.Setup(x => x.ExecuteAsync(It.IsAny<RadiusPipelineContext>()))
                .Callback(() =>
                {
                    callCount++;
                    if (callCount == 2) // Second server succeeds
                        return;
                    if (callCount > 2)
                        throw new Exception("Should not reach third server");
                })
                .Returns(Task.CompletedTask);

            // Act
            await _processor.ProcessPacketAsync(packet, clientConfig);

            // Assert
            pipelineMock.Verify(x => x.ExecuteAsync(It.IsAny<RadiusPipelineContext>()), Times.Exactly(2));
        }

        [Fact]
        public async Task ProcessPacketAsync_ShouldThrowPipelineNotFoundException_WhenPipelineNotFound()
        {
            // Arrange
            var header = new RadiusPacketHeader();
            var packet = new RadiusPacket(header);
            var clientConfig = new ClientConfiguration
            {
                Name = "TestClient"
            };

            _pipelineProviderMock.Setup(x => x.GetPipeline(clientConfig))
                .Returns((IRadiusPipeline)null);

            // Act & Assert
            await Assert.ThrowsAsync<PipelineNotFoundException>(() =>
                _processor.ProcessPacketAsync(packet, clientConfig));
        }

        [Fact]
        public async Task ProcessPacketAsync_ShouldSendResponse_WhenPipelineExecutesSuccessfully()
        {
            // Arrange
            var header = new RadiusPacketHeader();
            var packet = new RadiusPacket(header);
            var clientConfig = new ClientConfiguration
            {
                Name = "TestClient",
                LdapServers = new List<LdapServerConfiguration>()
            };

            var pipelineMock = new Mock<IRadiusPipeline>();
            _pipelineProviderMock.Setup(x => x.GetPipeline(clientConfig))
                .Returns(pipelineMock.Object);

            // Act
            await _processor.ProcessPacketAsync(packet, clientConfig);

            // Assert
            _responseSenderMock.Verify(x => x.SendResponse(It.IsAny<SendAdapterResponseRequest>()), Times.Once);
        }

        [Fact]
        public async Task ProcessPacketAsync_ShouldCreateContextWithLdapServer_WhenLdapServerProvided()
        {
            // Arrange
            var header = new RadiusPacketHeader();
            var packet = new RadiusPacket(header);
            packet.AddAttributeValue("User-Password", "password");
            var clientConfig = new ClientConfiguration
            {
                Name = "TestClient",
                RadiusSharedSecret = "secret",
                LdapServers = new List<LdapServerConfiguration>
                {
                    new LdapServerConfiguration { ConnectionString = "ldap://test-server" }
                }
            };

            var pipelineMock = new Mock<IRadiusPipeline>();
            _pipelineProviderMock.Setup(x => x.GetPipeline(clientConfig))
                .Returns(pipelineMock.Object);

            RadiusPipelineContext capturedContext = null;
            pipelineMock.Setup(x => x.ExecuteAsync(It.IsAny<RadiusPipelineContext>()))
                .Callback<RadiusPipelineContext>(ctx => capturedContext = ctx)
                .Returns(Task.CompletedTask);

            // Act
            await _processor.ProcessPacketAsync(packet, clientConfig);

            // Assert
            Assert.NotNull(capturedContext);
            Assert.NotNull(capturedContext.LdapConfiguration);
            Assert.Equal("ldap://test-server", capturedContext.LdapConfiguration.ConnectionString);
        }

        [Fact]
        public async Task ProcessPacketAsync_ShouldCreateContextWithoutLdapServer_WhenNoLdapServers()
        {
            // Arrange
            var header = new RadiusPacketHeader();
            var packet = new RadiusPacket(header);
            var clientConfig = new ClientConfiguration
            {
                Name = "TestClient",
                LdapServers = new List<LdapServerConfiguration>()
            };

            var pipelineMock = new Mock<IRadiusPipeline>();
            _pipelineProviderMock.Setup(x => x.GetPipeline(clientConfig))
                .Returns(pipelineMock.Object);

            RadiusPipelineContext capturedContext = null;
            pipelineMock.Setup(x => x.ExecuteAsync(It.IsAny<RadiusPipelineContext>()))
                .Callback<RadiusPipelineContext>(ctx => capturedContext = ctx)
                .Returns(Task.CompletedTask);

            // Act
            await _processor.ProcessPacketAsync(packet, clientConfig);

            // Assert
            Assert.NotNull(capturedContext);
            Assert.Null(capturedContext.LdapConfiguration);
        }

        [Fact]
        public async Task ProcessPacketAsync_ShouldParsePassphrase_WithPreAuthenticationMethod()
        {
            // Arrange
            var header = new RadiusPacketHeader();
            var packet = new RadiusPacket(header);
            var clientConfig = new ClientConfiguration
            {
                Name = "TestClient",
                PreAuthenticationMethod = PreAuthMode.Otp,
                LdapServers = new List<LdapServerConfiguration>()
            };

            var pipelineMock = new Mock<IRadiusPipeline>();
            _pipelineProviderMock.Setup(x => x.GetPipeline(clientConfig))
                .Returns(pipelineMock.Object);

            RadiusPipelineContext capturedContext = null;
            pipelineMock.Setup(x => x.ExecuteAsync(It.IsAny<RadiusPipelineContext>()))
                .Callback<RadiusPipelineContext>(ctx => capturedContext = ctx)
                .Returns(Task.CompletedTask);

            // Act
            await _processor.ProcessPacketAsync(packet, clientConfig);

            // Assert
            Assert.NotNull(capturedContext);
            Assert.NotNull(capturedContext.Passphrase);
        }
    }
}