using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MultiFactor.Radius.Adapter.Core.Framework.Context;
using MultiFactor.Radius.Adapter.Core.Radius;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration.ClientLevel;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration.Features.PreAuthModeFeature;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration.Features.UserNameTransform;
using MultiFactor.Radius.Adapter.Server.Pipeline.FirstFactorAuthentication.Processing;
using MultiFactor.Radius.Adapter.Services.Ldap;

namespace MultiFactor.Radius.Adapter.Tests;

public class LdapFirstFactorAuthenticationProcessorTests
{
        [Theory]
        [InlineData(AccountType.Unknown)]
        [InlineData(AccountType.Local)]
        [InlineData(AccountType.Microsoft)]
        public async Task WinLogonAccount_ShouldNotInvokeVerifyMembership(AccountType accountType)
        {
            var packetMock = new Mock<IRadiusPacket>();
            packetMock.Setup(x => x.UserName).Returns("user");
            packetMock.Setup(x => x.TryGetUserPassword()).Returns("password");
            packetMock.Setup(x => x.AccountType).Returns(accountType);
            var configMock = new Mock<IClientConfiguration>();
            configMock.Setup(x => x.UserNameTransformRules).Returns(new UserNameTransformRules());
            configMock.Setup(x => x.SplittedActiveDirectoryDomains).Returns(new[]{"localhost"});
            configMock.Setup(x => x.PreAuthnMode).Returns(PreAuthModeDescriptor.Default);
            var ldapServiceMock = new Mock<ILdapService>();
            
            var context = new RadiusContext(packetMock.Object, configMock.Object, new Mock<IServiceProvider>().Object);
            
            var processor = new LdapFirstFactorAuthenticationProcessor(ldapServiceMock.Object, NullLogger<LdapFirstFactorAuthenticationProcessor>.Instance);
            await processor.ProcessFirstAuthFactorAsync(context);
            ldapServiceMock.Verify(x => x.VerifyMembership(It.IsAny<string>(), It.IsAny<string>(),It.IsAny<string>(),It.IsAny<RadiusContext>()), Times.Never);
        }
        
        [Fact]
        public async Task WinLogonAccount_Domain_ShouldInvokeVerifyMembership()
        {
            var packetMock = new Mock<IRadiusPacket>();
            packetMock.Setup(x => x.UserName).Returns("user");
            packetMock.Setup(x => x.TryGetUserPassword()).Returns("password");
            packetMock.Setup(x => x.AccountType).Returns(AccountType.Domain); 
            var configMock = new Mock<IClientConfiguration>();
            configMock.Setup(x => x.UserNameTransformRules).Returns(new UserNameTransformRules());
            configMock.Setup(x => x.SplittedActiveDirectoryDomains).Returns(new[]{"localhost"});
            configMock.Setup(x => x.PreAuthnMode).Returns(PreAuthModeDescriptor.Default);
            var ldapServiceMock = new Mock<ILdapService>();
            
            var context = new RadiusContext(packetMock.Object, configMock.Object, new Mock<IServiceProvider>().Object);
            
            var processor = new LdapFirstFactorAuthenticationProcessor(ldapServiceMock.Object, NullLogger<LdapFirstFactorAuthenticationProcessor>.Instance);
            await processor.ProcessFirstAuthFactorAsync(context);
            ldapServiceMock.Verify(x => x.VerifyMembership(It.IsAny<string>(), It.IsAny<string>(),It.IsAny<string>(),It.IsAny<RadiusContext>()), Times.Once);
        }
}