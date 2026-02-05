// using Microsoft.Extensions.Logging;
// using Moq;
// using Multifactor.Core.Ldap.Name;
// using Multifactor.Core.Ldap.Schema;
// using Multifactor.Radius.Adapter.v2.Application.Configuration.Models;
// using Multifactor.Radius.Adapter.v2.Application.Features.Ldap.Models;
// using Multifactor.Radius.Adapter.v2.Application.Features.Ldap.Ports;
// using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.Models;
// using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.Models.Enum;
// using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.Steps;
// using Multifactor.Radius.Adapter.v2.Application.Features.Radius.Models;
// using Multifactor.Radius.Adapter.v2.Application.Features.Radius.Models.Enums;
// using Multifactor.Radius.Adapter.v2.Infrastructure.Configurations.Models;
//
// namespace Multifactor.Radius.Adapter.v2.Tests.Application.Pipeline.Steps
// {
//     public class UserGroupLoadingStepTests
//     {
//         private readonly Mock<ILdapAdapter> _ldapAdapterMock;
//         private readonly UserGroupLoadingStep _step;
//
//         public UserGroupLoadingStepTests()
//         {
//             _ldapAdapterMock = new Mock<ILdapAdapter>();
//             var loggerMock = new Mock<ILogger<UserGroupLoadingStep>>();
//             
//             _step = new UserGroupLoadingStep(
//                 _ldapAdapterMock.Object,
//                 loggerMock.Object);
//         }
//
//         [Fact]
//         public async Task ExecuteAsync_WhenRequestNotAccepted_Skips()
//         {
//             // Arrange
//             var context = CreateTestContext();
//             context.FirstFactorStatus = AuthenticationStatus.Reject; // Not accepted
//
//             // Act
//             await _step.ExecuteAsync(context);
//
//             // Assert
//             Assert.Null(context.UserGroups);
//             _ldapAdapterMock.Verify(x => x.LoadUserGroups(It.IsAny<LoadUserGroupRequest>()), Times.Never);
//         }
//
//         [Fact]
//         public async Task ExecuteAsync_WhenGroupsNotRequired_Skips()
//         {
//             // Arrange
//             var context = CreateTestContext();
//             context.FirstFactorStatus = AuthenticationStatus.Accept;
//             context.SecondFactorStatus = AuthenticationStatus.Accept;
//             // No reply attributes require groups
//
//             // Act
//             await _step.ExecuteAsync(context);
//
//             // Assert
//             Assert.Null(context.UserGroups);
//             _ldapAdapterMock.Verify(x => x.LoadUserGroups(It.IsAny<LoadUserGroupRequest>()), Times.Never);
//         }
//
//         [Fact]
//         public async Task ExecuteAsync_WhenLocalAccount_Skips()
//         {
//             // Arrange
//             var context = CreateTestContext();
//             context.FirstFactorStatus = AuthenticationStatus.Accept;
//             context.SecondFactorStatus = AuthenticationStatus.Accept;
//             context.RequestPacket.AddAttributeValue("Acct-Authentic", new[]{2}); //local
//
//
//             // Act
//             await _step.ExecuteAsync(context);
//
//             // Assert
//             Assert.Null(context.UserGroups);
//             _ldapAdapterMock.Verify(x => x.LoadUserGroups(It.IsAny<LoadUserGroupRequest>()), Times.Never);
//         }
//
//         [Fact]
//         public async Task ExecuteAsync_WhenAccepted_LoadsGroupsFromProfile()
//         {
//             // Arrange
//             var context = CreateTestContext();
//             context.FirstFactorStatus = AuthenticationStatus.Accept;
//             context.SecondFactorStatus = AuthenticationStatus.Accept;
//             
//             // Add reply attribute that requires groups
//             var replyAttribute = new RadiusReplyAttribute { IsMemberOf = true };
//             context.ClientConfiguration.ReplyAttributes = new Dictionary<string, RadiusReplyAttribute[]>
//             {
//                 ["Test"] = new[] { replyAttribute }
//             };
//
//             // Setup memberOf groups
//             var group1 = new DistinguishedName("CN=Group1,DC=test,DC=com");
//             var group2 = new DistinguishedName("CN=Group2,DC=test,DC=com");
//             context.LdapProfile.MemberOf = new List<DistinguishedName> { group1, group2 };
//
//             // Act
//             await _step.ExecuteAsync(context);
//
//             // Assert
//             Assert.NotNull(context.UserGroups);
//             Assert.Equal(2, context.UserGroups.Count);
//             Assert.Contains("Group1", context.UserGroups);
//             Assert.Contains("Group2", context.UserGroups);
//         }
//
//         [Fact]
//         public async Task ExecuteAsync_WhenNestedGroupsDisabled_DoesNotLoadFromLdap()
//         {
//             // Arrange
//             var context = CreateTestContext();
//             context.FirstFactorStatus = AuthenticationStatus.Accept;
//             context.SecondFactorStatus = AuthenticationStatus.Accept;
//             context.LdapConfiguration.LoadNestedGroups = false;
//             
//             var replyAttribute = new RadiusReplyAttribute { IsMemberOf = true };
//             context.ClientConfiguration.ReplyAttributes = new Dictionary<string, RadiusReplyAttribute[]>
//             {
//                 ["Test"] = new[] { replyAttribute }
//             };
//
//             // Act
//             await _step.ExecuteAsync(context);
//
//             // Assert
//             _ldapAdapterMock.Verify(x => x.LoadUserGroups(It.IsAny<LoadUserGroupRequest>()), Times.Never);
//         }
//
//         [Fact]
//         public async Task ExecuteAsync_WhenNestedGroupsEnabledAndBaseDns_LoadsFromContainers()
//         {
//             // Arrange
//             var context = CreateTestContext();
//             context.FirstFactorStatus = AuthenticationStatus.Accept;
//             context.SecondFactorStatus = AuthenticationStatus.Accept;
//             context.LdapConfiguration.LoadNestedGroups = true;
//             context.LdapConfiguration.NestedGroupsBaseDns = new List<DistinguishedName>
//             {
//                 new("OU=Groups,DC=test,DC=com"),
//                 new("OU=Security,DC=test,DC=com")
//             };
//
//             var replyAttribute = new Mock<RadiusReplyAttribute>();
//             replyAttribute.Setup(x => x.IsMemberOf).Returns(true);
//             
//             context.ClientConfiguration.ReplyAttributes = new Dictionary<string, IRadiusReplyAttribute[]>
//             {
//                 ["Test"] = [replyAttribute.Object]
//             };
//
//             _ldapAdapterMock
//                 .SetupSequence(x => x.LoadUserGroups(It.IsAny<LoadUserGroupRequest>()))
//                 .Returns(new List<string> { "NestedGroup1", "NestedGroup2" })
//                 .Returns(new List<string> { "SecurityGroup1" });
//
//             // Act
//             await _step.ExecuteAsync(context);
//
//             // Assert
//             _ldapAdapterMock.Verify(x => x.LoadUserGroups(It.IsAny<LoadUserGroupRequest>()), Times.Exactly(2));
//             
//             Assert.NotNull(context.UserGroups);
//             Assert.Contains("NestedGroup1", context.UserGroups);
//             Assert.Contains("NestedGroup2", context.UserGroups);
//             Assert.Contains("SecurityGroup1", context.UserGroups);
//         }
//
//         [Fact]
//         public async Task ExecuteAsync_WhenNestedGroupsEnabledNoBaseDns_LoadsFromRoot()
//         {
//             // Arrange
//             var context = CreateTestContext();
//             context.FirstFactorStatus = AuthenticationStatus.Accept;
//             context.SecondFactorStatus = AuthenticationStatus.Accept;
//             context.LdapConfiguration.LoadNestedGroups = true;
//             context.LdapConfiguration.NestedGroupsBaseDns = new List<DistinguishedName>(); // Empty
//             
//             var replyAttribute = new RadiusReplyAttribute { IsMemberOf = true };
//             context.ClientConfiguration.ReplyAttributes = new Dictionary<string, RadiusReplyAttribute[]>
//             {
//                 ["Test"] = new[] { replyAttribute }
//             };
//
//             _ldapAdapterMock
//                 .Setup(x => x.LoadUserGroups(It.IsAny<LoadUserGroupRequest>()))
//                 .Returns(new List<string> { "RootGroup1", "RootGroup2" });
//
//             // Act
//             await _step.ExecuteAsync(context);
//
//             // Assert
//             _ldapAdapterMock.Verify(x => x.LoadUserGroups(It.IsAny<LoadUserGroupRequest>()), Times.Once);
//             
//             Assert.NotNull(context.UserGroups);
//             Assert.Contains("RootGroup1", context.UserGroups);
//             Assert.Contains("RootGroup2", context.UserGroups);
//         }
//
//         [Fact]
//         public async Task ExecuteAsync_WhenUserGroupConditionInReplyAttributes_GroupsRequired()
//         {
//             // Arrange
//             var context = CreateTestContext();
//             context.FirstFactorStatus = AuthenticationStatus.Accept;
//             context.SecondFactorStatus = AuthenticationStatus.Accept;
//             
//             // Reply attribute with user group condition
//             var replyAttribute = new RadiusReplyAttribute 
//             { 
//                 UserGroupCondition = new List<string> { "AdminGroup" }
//             };
//             context.ClientConfiguration.ReplyAttributes = new Dictionary<string, RadiusReplyAttribute[]>
//             {
//                 ["Test"] = new[] { replyAttribute }
//             };
//
//             // Act
//             await _step.ExecuteAsync(context);
//
//             // Assert
//             Assert.NotNull(context.UserGroups);
//             // Groups should be loaded even though IsMemberOf is false
//         }
//
//         private static RadiusPipelineContext CreateTestContext()
//         {
//             var clientConfig = new ClientConfiguration
//             {
//                 Name = "test-client",
//                 RadiusSharedSecret = "test-secret",
//                 ReplyAttributes = new Dictionary<string, RadiusReplyAttribute[]>()
//             };
//
//             var ldapConfig = new LdapServerConfiguration
//             {
//                 ConnectionString = "ldap://test.com",
//                 Username = "admin",
//                 Password = "password",
//                 BindTimeoutSeconds = 30,
//                 LoadNestedGroups = false,
//                 NestedGroupsBaseDns = new List<DistinguishedName>()
//             };
//
//             var requestPacket = new RadiusPacket(
//                 new RadiusPacketHeader(PacketCode.AccessRequest, 1, new byte[16]))
//             {
//                 UserName = "testuser",
//                 AccountType = AccountType.Domain
//             };
//
//             var ldapSchemaMock = new Mock<ILdapSchema>();
//             var ldapProfileMock = new Mock<ILdapProfile>();
//             var userDnMock = new Mock<DistinguishedName>();
//             userDnMock.Setup(x => x.StringRepresentation).Returns("CN=testuser,DC=test,DC=com");
//             ldapProfileMock.Setup(x => x.Dn).Returns(userDnMock.Object);
//             ldapProfileMock.Setup(x => x.MemberOf).Returns(new List<DistinguishedName>());
//
//             var context = new RadiusPipelineContext(requestPacket, clientConfig, ldapConfig);
//             context.LdapSchema = ldapSchemaMock.Object;
//             context.LdapProfile = ldapProfileMock.Object;
//             
//             return context;
//         }
//     }
// }