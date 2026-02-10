using System.DirectoryServices.Protocols;
using System.Net;
using Microsoft.Extensions.Logging;
using Moq;
using Multifactor.Core.Ldap.Schema;
using Multifactor.Radius.Adapter.v2.Application.Core.Models.Enum;
using Multifactor.Radius.Adapter.v2.Application.Features.Ldap.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.Ldap.Ports;
using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.FirstFactor;
using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.FirstFactor.BindNameFormat;
using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.Models.Enum;
using Multifactor.Radius.Adapter.v2.Application.Features.Radius.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.Radius.Models.Enums;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configurations.Models;

namespace Multifactor.Radius.Adapter.v2.Tests.Application.Pipeline.FirstFactor
{
    public class LdapFirstFactorProcessorTests
    {
        private readonly Mock<ILdapBindNameFormatterProvider> _formatterProviderMock;
        private readonly Mock<ILogger<LdapFirstFactorProcessor>> _loggerMock;
        private readonly Mock<ILdapAdapter> _ldapAdapterMock;
        private readonly LdapFirstFactorProcessor _processor;

        public LdapFirstFactorProcessorTests()
        {
            _formatterProviderMock = new Mock<ILdapBindNameFormatterProvider>();
            _loggerMock = new Mock<ILogger<LdapFirstFactorProcessor>>();
            _ldapAdapterMock = new Mock<ILdapAdapter>();
            
            _processor = new LdapFirstFactorProcessor(
                _formatterProviderMock.Object,
                _loggerMock.Object,
                _ldapAdapterMock.Object);
        }

        [Fact]
        public void AuthenticationSource_ShouldBeLdap()
        {
            // Act & Assert
            Assert.Equal(AuthenticationSource.Ldap, _processor.AuthenticationSource);
        }

        [Fact]
        public async Task ProcessFirstFactor_ShouldRejectWhenUserNameMissing()
        {
            // Arrange
            var requestPacket = CreateRadiusPacket(userName: null);
            var context = CreateContext(requestPacket);

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
        public async Task ProcessFirstFactor_ShouldRejectWhenPasswordMissing()
        {
            // Arrange
            var requestPacket = CreateRadiusPacket("testuser");
            var context = CreateContext(requestPacket);
            context.Passphrase = UserPassphrase.Parse("", PreAuthMode.None);

            // Act
            await _processor.ProcessFirstFactor(context);

            // Assert
            Assert.Equal(AuthenticationStatus.Reject, context.FirstFactorStatus);
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("No User-Password")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task ProcessFirstFactor_ShouldAcceptWhenLdapBindSucceeds()
        {
            // Arrange
            var requestPacket = CreateRadiusPacket("testuser");
            var context = CreateContext(requestPacket);
            
            _ldapAdapterMock
                .Setup(x => x.CheckConnection(It.IsAny<LdapConnectionData>()))
                .Returns(true);

            // Act
            await _processor.ProcessFirstFactor(context);

            // Assert
            Assert.Equal(AuthenticationStatus.Accept, context.FirstFactorStatus);
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
        public async Task ProcessFirstFactor_ShouldRejectWhenLdapBindFails()
        {
            // Arrange
            var requestPacket = CreateRadiusPacket("testuser");
            var context = CreateContext(requestPacket);
            
            _ldapAdapterMock
                .Setup(x => x.CheckConnection(It.IsAny<LdapConnectionData>()))
                .Returns(false);

            // Act
            await _processor.ProcessFirstFactor(context);

            // Assert
            Assert.Equal(AuthenticationStatus.Reject, context.FirstFactorStatus);
        }

        [Fact]
        public async Task ProcessFirstFactor_ShouldHandleLdapException()
        {
            // Arrange
            var requestPacket = CreateRadiusPacket("testuser");
            var context = CreateContext(requestPacket);
            
            var ldapException = new LdapException(52, "Bind failed", "data 52e");
            
            _ldapAdapterMock
                .Setup(x => x.CheckConnection(It.IsAny<LdapConnectionData>()))
                .Throws(ldapException);

            // Act
            await _processor.ProcessFirstFactor(context);

            // Assert
            Assert.Equal(AuthenticationStatus.Reject, context.FirstFactorStatus);
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("InvalidCredentials")),
                    It.IsAny<LdapException>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()!),
                Times.Once);
        }

        [Fact]
        public async Task ProcessFirstFactor_ShouldSetMustChangePasswordOnSpecificErrors()
        {
            // Arrange
            var requestPacket = CreateRadiusPacket("testuser");
            var context = CreateContext(requestPacket);
            
            var ldapException = new LdapException(532 ,"Password expired", "data 532");
            
            _ldapAdapterMock
                .Setup(x => x.CheckConnection(It.IsAny<LdapConnectionData>()))
                .Throws(ldapException);

            // Act
            await _processor.ProcessFirstFactor(context);

            // Assert
            Assert.Equal(AuthenticationStatus.Reject, context.FirstFactorStatus);
            Assert.Equal(context.LdapConfiguration.ConnectionString, context.MustChangePasswordDomain);
        }

        [Fact]
        public async Task ProcessFirstFactor_ShouldThrowWhenLdapConfigurationMissing()
        {
            // Arrange
            var requestPacket = CreateRadiusPacket("testuser");
            var context = CreateContextWithoutLdap(requestPacket);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _processor.ProcessFirstFactor(context));
        }

        private RadiusPacket CreateRadiusPacket(string userName)
        {
            var header = new RadiusPacketHeader(PacketCode.AccessRequest, 1, new byte[16]);
            var packet = new RadiusPacket(header)
            {
                RemoteEndpoint = new IPEndPoint(IPAddress.Any, 228),
            };
            
            if (userName != null)
            {
                packet.AddAttributeValue("User-Name", userName);
                packet.AddAttributeValue("User-Password", "password");
            }
            
            return packet;
        }

        private RadiusPipelineContext CreateContext(RadiusPacket requestPacket)
        {
            var clientConfig = new ClientConfiguration
            {
                Name = "TestClient"
            };
            
            var ldapConfig = new LdapServerConfiguration
            {
                ConnectionString = "ldap://test.domain.com",
                Username = "admin",
                Password = "admin-pass",
                BindTimeoutSeconds = 30
            };
            
            var context = new RadiusPipelineContext(requestPacket, clientConfig, ldapConfig)
            {
                Passphrase = UserPassphrase.Parse("password", PreAuthMode.None),
                LdapSchema = LdapSchemaBuilder.Default
            };
            
            return context;
        }

        private RadiusPipelineContext CreateContextWithoutLdap(RadiusPacket requestPacket)
        {
            var clientConfig = new ClientConfiguration
            {
                Name = "TestClient"
            };
            
            var context = new RadiusPipelineContext(requestPacket, clientConfig)
            {
                Passphrase = UserPassphrase.Parse("password", PreAuthMode.None),
                LdapSchema = LdapSchemaBuilder.Default
            };
            
            return context;
        }
    }
}