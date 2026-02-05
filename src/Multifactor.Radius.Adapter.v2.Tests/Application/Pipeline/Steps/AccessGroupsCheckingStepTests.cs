// using Microsoft.Extensions.Logging;
// using Moq;
// using Multifactor.Core.Ldap.Name;
// using Multifactor.Core.Ldap.Schema;
// using Multifactor.Radius.Adapter.v2.Application.Features.Ldap.Models;
// using Multifactor.Radius.Adapter.v2.Application.Features.Ldap.Ports;
// using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.Models;
// using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.Models.Enum;
// using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.Steps;
// using Multifactor.Radius.Adapter.v2.Application.Features.Radius.Models;
// using Multifactor.Radius.Adapter.v2.Infrastructure.Configurations.Models;
//
// namespace Multifactor.Radius.Adapter.v2.Tests.Application.Pipeline.Steps
// {
//     public class AccessGroupsCheckingStepTests
//     {
//         private readonly Mock<ILdapAdapter> _ldapAdapterMock;
//         private readonly AccessGroupsCheckingStep _step;
//
//         public AccessGroupsCheckingStepTests()
//         {
//             _ldapAdapterMock = new Mock<ILdapAdapter>();
//             var loggerMock = new Mock<ILogger<AccessGroupsCheckingStep>>();
//             _step = new AccessGroupsCheckingStep(_ldapAdapterMock.Object, loggerMock.Object);
//         }
//
//         [Fact]
//         public async Task ExecuteAsync_ShouldThrowArgumentNullException_WhenContextIsNull()
//         {
//             // Act & Assert
//             await Assert.ThrowsAsync<ArgumentNullException>(() => _step.ExecuteAsync(null));
//         }
//
//         [Fact]
//         public async Task ExecuteAsync_ShouldThrowArgumentNullException_WhenLdapConfigurationIsNull()
//         {
//             // Arrange
//             var context = new RadiusPipelineContext(It.IsAny<RadiusPacket>(), It.IsAny<ClientConfiguration>());
//
//             // Act & Assert
//             await Assert.ThrowsAsync<ArgumentNullException>(() => _step.ExecuteAsync(context));
//         }
//
//         [Fact]
//         public async Task ExecuteAsync_ShouldThrowArgumentNullException_WhenLdapSchemaIsNull()
//         {
//             // Arrange
//             var context = new RadiusPipelineContext(It.IsAny<RadiusPacket>(), 
//                 It.IsAny<ClientConfiguration>(),
//                 It.IsAny<LdapServerConfiguration>())
//             {
//                 LdapSchema = null
//             };
//
//             // Act & Assert
//             await Assert.ThrowsAsync<ArgumentNullException>(() => _step.ExecuteAsync(context));
//         }
//
//         [Fact]
//         public async Task ExecuteAsync_ShouldSkip_WhenNoAccessGroups()
//         {
//             // Arrange
//             var ldapConf = new LdapServerConfiguration()
//             {
//                 AccessGroups = []
//             };
//             var context = new RadiusPipelineContext(It.IsAny<RadiusPacket>(),
//                 It.IsAny<ClientConfiguration>(),
//                 ldapConf)
//             {
//                 LdapSchema = It.IsAny<ILdapSchema>(),
//                 LdapProfile = It.IsAny<ILdapProfile>()
//             };
//
//             // Act
//             await _step.ExecuteAsync(context);
//
//             // Assert
//             Assert.False(context.IsTerminated);
//         }
//
//         [Fact]
//         public async Task ExecuteAsync_ShouldSkip_WhenUnsupportedAccountType()
//         {
//             // Arrange
//             var requestPacket = new RadiusPacket(It.IsAny<RadiusPacketHeader>())
//             {
//                 UserName = "testuser",
//                 AccountType = "local"
//             };
//             var ldapConf = new LdapServerConfiguration()
//             {
//                 AccessGroups = new List<DistinguishedName> { new ("group1") }
//             };
//             var context = new RadiusPipelineContext(requestPacket, It.IsAny<ClientConfiguration>(),
//                 ldapConf)
//             {
//                 LdapSchema = It.IsAny<ILdapSchema>(),
//                 LdapProfile = new LdapProfile(),
//                 IsDomainAccount = false,
//             };
//
//             // Act
//             await _step.ExecuteAsync(context);
//
//             // Assert
//             Assert.False(context.IsTerminated);
//         }
//
//         [Fact]
//         public async Task ExecuteAsync_ShouldThrowArgumentNullException_WhenLdapProfileIsNull()
//         {
//             // Arrange
//             var context = new RadiusPipelineContext
//             {
//                 LdapConfiguration = new LdapServerConfiguration
//                 {
//                     AccessGroups = new List<string> { "group1" }
//                 },
//                 LdapSchema = It.IsAny<ILdapSchema>(),
//                 LdapProfile = null,
//                 IsDomainAccount = true
//             };
//
//             // Act & Assert
//             await Assert.ThrowsAsync<ArgumentNullException>(() => _step.ExecuteAsync(context));
//         }
//
//         [Fact]
//         public async Task ExecuteAsync_ShouldContinue_WhenUserIsMemberOfAccessGroupViaProfile()
//         {
//             // Arrange
//             var context = new RadiusPipelineContext
//             {
//                 LdapConfiguration = new LdapServerConfiguration
//                 {
//                     AccessGroups = new List<string> { "group1", "group2" }
//                 },
//                 LdapSchema = It.IsAny<ILdapSchema>(),
//                 LdapProfile = new LdapProfile
//                 {
//                     Dn = "cn=user,dc=test",
//                     MemberOf = new List<string> { "group2", "group3" }
//                 },
//                 IsDomainAccount = true
//             };
//
//             // Act
//             await _step.ExecuteAsync(context);
//
//             // Assert
//             Assert.False(context.IsTerminated);
//             Assert.NotEqual(AuthenticationStatus.Reject, context.FirstFactorStatus);
//             Assert.NotEqual(AuthenticationStatus.Reject, context.SecondFactorStatus);
//         }
//
//         [Fact]
//         public async Task ExecuteAsync_ShouldContinue_WhenLdapAdapterConfirmsMembership()
//         {
//             // Arrange
//             var context = new RadiusPipelineContext
//             {
//                 LdapConfiguration = new LdapServerConfiguration
//                 {
//                     AccessGroups = new List<string> { "group1" },
//                     ConnectionString = "ldap://test"
//                 },
//                 LdapSchema = It.IsAny<ILdapSchema>(),
//                 LdapProfile = new LdapProfile
//                 {
//                     Dn = "cn=user,dc=test",
//                     MemberOf = new List<string> { "group3" }
//                 },
//                 IsDomainAccount = true
//             };
//
//             _ldapAdapterMock
//                 .Setup(x => x.IsMemberOf(It.IsAny<MembershipRequest>()))
//                 .Returns(true);
//
//             // Act
//             await _step.ExecuteAsync(context);
//
//             // Assert
//             Assert.False(context.IsTerminated);
//             _ldapAdapterMock.Verify(x => x.IsMemberOf(It.IsAny<MembershipRequest>()), Times.Once);
//         }
//
//         [Fact]
//         public async Task ExecuteAsync_ShouldTerminate_WhenUserIsNotMemberOfAnyAccessGroup()
//         {
//             // Arrange
//             var context = new RadiusPipelineContext
//             {
//                 LdapConfiguration = new LdapServerConfiguration()
//                 {
//                     AccessGroups = new List<string> { "group1" },
//                     ConnectionString = "ldap://test"
//                 },
//                 LdapProfile = new LdapProfile
//                 {
//                     Dn = "cn=user,dc=test",
//                     MemberOf = new List<string> { "group2", "group3" }
//                 },
//                 IsDomainAccount = true
//             };
//
//             _ldapAdapterMock
//                 .Setup(x => x.IsMemberOf(It.IsAny<MembershipRequest>()))
//                 .Returns(false);
//
//             // Act
//             await _step.ExecuteAsync(context);
//
//             // Assert
//             Assert.True(context.IsTerminated);
//             Assert.Equal(AuthenticationStatus.Reject, context.FirstFactorStatus);
//             Assert.Equal(AuthenticationStatus.Reject, context.SecondFactorStatus);
//             _ldapAdapterMock.Verify(x => x.IsMemberOf(It.IsAny<MembershipRequest>()), Times.Once);
//         }
//     }
// }