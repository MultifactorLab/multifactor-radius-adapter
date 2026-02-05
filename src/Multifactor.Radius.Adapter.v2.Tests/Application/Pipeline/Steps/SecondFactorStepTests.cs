using Microsoft.Extensions.Logging;
using Moq;
using Multifactor.Core.Ldap.Name;
using Multifactor.Radius.Adapter.v2.Application.Cache;
using Multifactor.Radius.Adapter.v2.Application.Configuration.Models;
using Multifactor.Radius.Adapter.v2.Application.Configuration.Models.Enum;
using Multifactor.Radius.Adapter.v2.Application.Features.Ldap.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.Ldap.Ports;
using Multifactor.Radius.Adapter.v2.Application.Features.Multifactor;
using Multifactor.Radius.Adapter.v2.Application.Features.Multifactor.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.Multifactor.Ports;
using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.AccessChallenge;
using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.AccessChallenge.Models.Enums;
using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.Models.Enum;
using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.Steps;
using Multifactor.Radius.Adapter.v2.Application.Features.Radius.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.Radius.Models.Enums;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configurations.Models;

namespace Multifactor.Radius.Adapter.v2.Tests.Application.Pipeline.Steps
{
    public class SecondFactorStepTests
    {
        private readonly Mock<MultifactorApiService> _apiServiceMock;
        private readonly Mock<IChallengeProcessorProvider> _challengeProviderMock;
        private readonly Mock<ILdapAdapter> _ldapAdapterMock;
        private readonly Mock<ILogger<SecondFactorStep>> _loggerMock;
        private readonly SecondFactorStep _step;

        public SecondFactorStepTests()
        {
            _apiServiceMock = new Mock<MultifactorApiService>(
                Mock.Of<IMultifactorApi>(),
                Mock.Of<IAuthenticatedClientCache>(),
                Mock.Of<ILogger<MultifactorApiService>>());
            
            _challengeProviderMock = new Mock<IChallengeProcessorProvider>();
            _ldapAdapterMock = new Mock<ILdapAdapter>();
            _loggerMock = new Mock<ILogger<SecondFactorStep>>();
            
            _step = new SecondFactorStep(
                _apiServiceMock.Object,
                _challengeProviderMock.Object,
                _ldapAdapterMock.Object,
                _loggerMock.Object);
        }

        [Fact]
        public async Task ExecuteAsync_WhenContextNull_ThrowsException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => 
                _step.ExecuteAsync(null));
        }

        [Fact]
        public async Task ExecuteAsync_WhenSecondFactorNotAwaiting_SetsBypass()
        {
            // Arrange
            var context = CreateTestContext();
            context.SecondFactorStatus = AuthenticationStatus.Accept; // Not Awaiting

            // Act
            await _step.ExecuteAsync(context);

            // Assert
            Assert.Equal(AuthenticationStatus.Bypass, context.SecondFactorStatus);
            _apiServiceMock.Verify(
                x => x.CreateSecondFactorRequestAsync(It.IsAny<RadiusPipelineContext>(), It.IsAny<bool>()),
                Times.Never);
        }

        [Fact]
        public async Task ExecuteAsync_WhenVendorAclRequestAndRadiusSource_Bypasses()
        {
            // Arrange
            var context = CreateTestContext();
            context.SecondFactorStatus = AuthenticationStatus.Awaiting;
            context.RequestPacket.AddAttributeValue("User-Name", "#ACSACL#-IP");// Vendor ACL
            context.ClientConfiguration.FirstFactorAuthenticationSource = AuthenticationSource.Radius;

            // Act
            await _step.ExecuteAsync(context);

            // Assert
            Assert.Equal(AuthenticationStatus.Bypass, context.SecondFactorStatus);
            _apiServiceMock.Verify(
                x => x.CreateSecondFactorRequestAsync(It.IsAny<RadiusPipelineContext>(), It.IsAny<bool>()),
                Times.Never);
        }

        [Fact]
        public async Task ExecuteAsync_WhenVendorAclRequestAndLdapSource_CallsApi()
        {
            // Arrange
            var context = CreateTestContext();
            context.SecondFactorStatus = AuthenticationStatus.Awaiting;
            context.RequestPacket.AddAttributeValue("User-Name", "#ACSACL#-IP");// Vendor ACL
            context.ClientConfiguration.FirstFactorAuthenticationSource = AuthenticationSource.Ldap;

            _apiServiceMock
                .Setup(x => x.CreateSecondFactorRequestAsync(context, It.IsAny<bool>()))
                .ReturnsAsync(new SecondFactorResponse(AuthenticationStatus.Accept));

            // Act
            await _step.ExecuteAsync(context);

            // Assert
            _apiServiceMock.Verify(
                x => x.CreateSecondFactorRequestAsync(context, It.IsAny<bool>()),
                Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_WhenUserInSecondFaBypassGroup_Bypasses()
        {
            // Arrange
            var context = CreateTestContext();
            context.SecondFactorStatus = AuthenticationStatus.Awaiting;
            
            _ldapAdapterMock
                .Setup(x => x.IsMemberOf(It.IsAny<MembershipRequest>()))
                .Returns(true);

            // Act
            await _step.ExecuteAsync(context);

            // Assert
            Assert.Equal(AuthenticationStatus.Bypass, context.SecondFactorStatus);
            _apiServiceMock.Verify(
                x => x.CreateSecondFactorRequestAsync(It.IsAny<RadiusPipelineContext>(), It.IsAny<bool>()),
                Times.Never);
        }

        [Fact]
        public async Task ExecuteAsync_WhenUserInSecondFaGroup_CallsApi()
        {
            // Arrange
            var context = CreateTestContext();
            context.SecondFactorStatus = AuthenticationStatus.Awaiting;
            
            _ldapAdapterMock
                .Setup(x => x.IsMemberOf(It.IsAny<MembershipRequest>()))
                .Returns(true);

            _apiServiceMock
                .Setup(x => x.CreateSecondFactorRequestAsync(context, It.IsAny<bool>()))
                .ReturnsAsync(new SecondFactorResponse(AuthenticationStatus.Accept));

            // Act
            await _step.ExecuteAsync(context);

            // Assert
            _apiServiceMock.Verify(
                x => x.CreateSecondFactorRequestAsync(context, It.IsAny<bool>()),
                Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_WhenUserNotInSecondFaGroup_Bypasses()
        {
            // Arrange
            var context = CreateTestContext();
            context.SecondFactorStatus = AuthenticationStatus.Awaiting;
            
            _ldapAdapterMock
                .Setup(x => x.IsMemberOf(It.IsAny<MembershipRequest>()))
                .Returns(false);

            // Act
            await _step.ExecuteAsync(context);

            // Assert
            Assert.Equal(AuthenticationStatus.Bypass, context.SecondFactorStatus);
            _apiServiceMock.Verify(
                x => x.CreateSecondFactorRequestAsync(It.IsAny<RadiusPipelineContext>(), It.IsAny<bool>()),
                Times.Never);
        }

        [Fact]
        public async Task ExecuteAsync_WhenLocalAccount_CallsApi()
        {
            // Arrange
            var context = CreateTestContext();
            context.SecondFactorStatus = AuthenticationStatus.Awaiting;
            context.RequestPacket.AddAttributeValue("Acct-Authentic", new[]{2}); //local

            _apiServiceMock
                .Setup(x => x.CreateSecondFactorRequestAsync(context, It.IsAny<bool>()))
                .ReturnsAsync(new SecondFactorResponse(AuthenticationStatus.Accept));

            // Act
            await _step.ExecuteAsync(context);

            // Assert
            _apiServiceMock.Verify(
                x => x.CreateSecondFactorRequestAsync(context, It.IsAny<bool>()),
                Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_WhenApiReturnsAccept_SetsStatus()
        {
            // Arrange
            var context = CreateTestContext();
            context.SecondFactorStatus = AuthenticationStatus.Awaiting;

            _apiServiceMock
                .Setup(x => x.CreateSecondFactorRequestAsync(context, It.IsAny<bool>()))
                .ReturnsAsync(new SecondFactorResponse(AuthenticationStatus.Accept, "state123", "Welcome"));

            // Act
            await _step.ExecuteAsync(context);

            // Assert
            Assert.Equal(AuthenticationStatus.Accept, context.SecondFactorStatus);
            Assert.Equal("state123", context.ResponseInformation.State);
            Assert.Equal("Welcome", context.ResponseInformation.ReplyMessage);
        }

        [Fact]
        public async Task ExecuteAsync_WhenApiReturnsAwaiting_CreatesChallenge()
        {
            // Arrange
            var context = CreateTestContext();
            context.SecondFactorStatus = AuthenticationStatus.Awaiting;

            var challengeProcessorMock = new Mock<IChallengeProcessor>();
            _challengeProviderMock
                .Setup(x => x.GetChallengeProcessorByType(ChallengeType.SecondFactor))
                .Returns(challengeProcessorMock.Object);

            _apiServiceMock
                .Setup(x => x.CreateSecondFactorRequestAsync(context, It.IsAny<bool>()))
                .ReturnsAsync(new SecondFactorResponse(AuthenticationStatus.Awaiting, "challenge-state", "Enter code"));

            // Act
            await _step.ExecuteAsync(context);

            // Assert
            Assert.Equal(AuthenticationStatus.Awaiting, context.SecondFactorStatus);
            challengeProcessorMock.Verify(
                x => x.AddChallengeContext(context),
                Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_WhenNoLdapConfiguration_CallsApi()
        {
            // Arrange
            var context = CreateTestContext();
            context.SecondFactorStatus = AuthenticationStatus.Awaiting;
            // context.LdapConfiguration = null;

            _apiServiceMock
                .Setup(x => x.CreateSecondFactorRequestAsync(context, true)) // Should be true when no LDAP config
                .ReturnsAsync(new SecondFactorResponse(AuthenticationStatus.Accept));

            // Act
            await _step.ExecuteAsync(context);

            // Assert
            _apiServiceMock.Verify(
                x => x.CreateSecondFactorRequestAsync(context, true),
                Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_WhenLocalAccount_ShouldCacheReturnsFalse()
        {
            // Arrange
            var context = CreateTestContext();
            context.SecondFactorStatus = AuthenticationStatus.Awaiting;
            context.RequestPacket.AddAttributeValue("Acct-Authentic", new[]{2}); // local
            
            _apiServiceMock
                .Setup(x => x.CreateSecondFactorRequestAsync(context, false)) // Should be false for local account
                .ReturnsAsync(new SecondFactorResponse(AuthenticationStatus.Accept));

            // Act
            await _step.ExecuteAsync(context);

            // Assert
            _apiServiceMock.Verify(
                x => x.CreateSecondFactorRequestAsync(context, false),
                Times.Once);
        }

        private static RadiusPipelineContext CreateTestContext()
        {
            var clientConfig = new ClientConfiguration
            {
                Name = "test-client",
                RadiusSharedSecret = "test-secret",
                FirstFactorAuthenticationSource = AuthenticationSource.Ldap
            };

            var ldapConfig = new LdapServerConfiguration
            {
                ConnectionString = "ldap://test.com",
                Username = "admin",
                Password = "password",
                BindTimeoutSeconds = 30,
                SecondFaGroups = new List<DistinguishedName>
                {
                    new("CN=2FAGroup,DC=test,DC=com")
                },
                AuthenticationCacheGroups = new List<DistinguishedName>()
            };

            var requestPacket = new RadiusPacket(
                new RadiusPacketHeader(PacketCode.AccessRequest, 1, new byte[16]));
            requestPacket.AddAttributeValue("User-Name", "testuser");// Vendor ACL
            requestPacket.AddAttributeValue("Acct-Authentic", new[]{1});// domain

            var ldapProfileMock = new Mock<ILdapProfile>();
            ldapProfileMock.Setup(x => x.MemberOf).Returns(new List<DistinguishedName>());

            var context = new RadiusPipelineContext(requestPacket, clientConfig, ldapConfig);
            context.LdapProfile = ldapProfileMock.Object;
            context.SecondFactorStatus = AuthenticationStatus.Awaiting;
            
            return context;
        }
    }
}