// using Microsoft.Extensions.DependencyInjection;
// using Microsoft.Extensions.Logging;
// using Moq;
// using Multifactor.Radius.Adapter.v2.Application.Configuration.Models;
// using Multifactor.Radius.Adapter.v2.Application.Configuration.Models.Enum;
// using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline;
// using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.Interfaces;
// using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.Steps;
//
// namespace Multifactor.Radius.Adapter.v2.Tests.Pipeline
// {
//     public class RadiusPipelineFactoryTests
//     {
//         private readonly Mock<IServiceProvider> _serviceProviderMock;
//         private readonly Mock<ILogger<IRadiusPipelineFactory>> _loggerMock;
//         private readonly RadiusPipelineFactory _factory;
//
//         public RadiusPipelineFactoryTests()
//         {
//             _serviceProviderMock = new Mock<IServiceProvider>();
//             _loggerMock = new Mock<ILogger<IRadiusPipelineFactory>>();
//             _factory = new RadiusPipelineFactory(_serviceProviderMock.Object, _loggerMock.Object);
//             
//             SetupDefaultStepMocks();
//         }
//
//         [Fact]
//         public void CreatePipeline_ShouldCreateBasicStepsForEmptyConfig()
//         {
//             // Arrange
//             var clientConfig = new ClientConfiguration
//             {
//                 Name = "TestClient",
//                 PreAuthenticationMethod = null,
//                 ReplyAttributes = new Dictionary<string, RadiusReplyAttribute[]>()
//             };
//
//             // Act
//             var pipeline = _factory.CreatePipeline(clientConfig);
//
//             // Assert
//             Assert.NotNull(pipeline);
//             VerifyStepCreated<StatusServerFilteringStep>(Times.Once());
//             VerifyStepCreated<IpWhiteListStep>(Times.Once());
//             VerifyStepCreated<AccessRequestFilteringStep>(Times.Once());
//             VerifyStepCreated<AccessChallengeStep>(Times.Once());
//             VerifyStepCreated<FirstFactorStep>(Times.Once());
//             VerifyStepCreated<SecondFactorStep>(Times.Once());
//         }
//
//         [Fact]
//         public void CreatePipeline_ShouldAddLdapStepsWhenLdapConfigured()
//         {
//             // Arrange
//             var clientConfig = new ClientConfiguration
//             {
//                 Name = "TestClient",
//                 LdapServers = new List<LdapServerConfiguration> { new() },
//                 PreAuthenticationMethod = null,
//                 ReplyAttributes = new Dictionary<string, RadiusReplyAttribute[]>()
//             };
//
//             // Act
//             _factory.CreatePipeline(clientConfig);
//
//             // Assert
//             VerifyStepCreated<UserNameValidationStep>(Times.Once());
//             VerifyStepCreated<LdapSchemaLoadingStep>(Times.Once());
//             VerifyStepCreated<ProfileLoadingStep>(Times.Once());
//             VerifyStepCreated<AccessGroupsCheckingStep>(Times.Once());
//         }
//
//         [Fact]
//         public void CreatePipeline_ShouldAddPreAuthStepsWhenPreAuthEnabled()
//         {
//             // Arrange
//             var clientConfig = new ClientConfiguration
//             {
//                 Name = "TestClient",
//                 LdapServers = new List<LdapServerConfiguration> { new() },
//                 PreAuthenticationMethod = PreAuthMode.Any,
//                 ReplyAttributes = new Dictionary<string, RadiusReplyAttribute[]>()
//             };
//
//             // Act
//             _factory.CreatePipeline(clientConfig);
//
//             // Assert
//             VerifyStepCreated<PreAuthCheckStep>(Times.Once());
//             VerifyStepCreated<SecondFactorStep>(Times.Once());
//             VerifyStepCreated<PreAuthPostCheck>(Times.Once());
//             VerifyStepCreated<FirstFactorStep>(Times.Once());
//         }
//
//         [Fact]
//         public void CreatePipeline_ShouldAddUserGroupLoadingStepWhenRequired()
//         {
//             // Arrange
//             var replyAttributes = new Dictionary<string, RadiusReplyAttribute[]>
//             {
//                 ["MemberOf"] = new[] { new RadiusReplyAttribute { Name = "memberOf" } }
//             };
//
//             var clientConfig = new ClientConfiguration
//             {
//                 Name = "TestClient",
//                 LdapServers = new List<LdapServerConfiguration> { new() },
//                 PreAuthenticationMethod = null,
//                 ReplyAttributes = replyAttributes
//             };
//
//             // Act
//             _factory.CreatePipeline(clientConfig);
//
//             // Assert
//             VerifyStepCreated<UserGroupLoadingStep>(Times.Once());
//         }
//
//         [Fact]
//         public void CreatePipeline_ShouldNotAddUserGroupLoadingStepWhenNotRequired()
//         {
//             // Arrange
//             var clientConfig = new ClientConfiguration
//             {
//                 Name = "TestClient",
//                 LdapServers = new List<LdapServerConfiguration> { new() },
//                 PreAuthenticationMethod = null,
//                 ReplyAttributes = new Dictionary<string, RadiusReplyAttribute[]>
//                 {
//                     ["Class"] = new[] { new RadiusReplyAttribute { Value = "Test" } }
//                 }
//             };
//
//             // Act
//             _factory.CreatePipeline(clientConfig);
//
//             // Assert
//             VerifyStepCreated<UserGroupLoadingStep>(Times.Never());
//         }
//
//         [Fact]
//         public void CreatePipeline_ShouldLogPipelineCreation()
//         {
//             // Arrange
//             var clientConfig = new ClientConfiguration
//             {
//                 Name = "TestClient"
//             };
//
//             // Act
//             _factory.CreatePipeline(clientConfig);
//
//             // Assert
//             _loggerMock.Verify(
//                 x => x.Log(
//                     LogLevel.Debug,
//                     It.IsAny<EventId>(),
//                     It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Configuration: TestClient")),
//                     It.IsAny<Exception>(),
//                     It.IsAny<Func<It.IsAnyType, Exception, string>>()),
//                 Times.Once);
//         }
//
//         private void SetupDefaultStepMocks()
//         {
//             var steps = new Mock[]
//             {
//                 new Mock<StatusServerFilteringStep>(),
//                 new Mock<IpWhiteListStep>(),
//                 new Mock<AccessRequestFilteringStep>(),
//                 new Mock<UserNameValidationStep>(),
//                 new Mock<LdapSchemaLoadingStep>(),
//                 new Mock<ProfileLoadingStep>(),
//                 new Mock<AccessGroupsCheckingStep>(),
//                 new Mock<AccessChallengeStep>(),
//                 new Mock<PreAuthCheckStep>(),
//                 new Mock<SecondFactorStep>(),
//                 new Mock<PreAuthPostCheck>(),
//                 new Mock<FirstFactorStep>(),
//                 new Mock<UserGroupLoadingStep>()
//             };
//             _serviceProviderMock
//                 .Setup(x => x.GetService<It.IsAnyType>())
//                 .Returns<IRadiusPipelineStep>(type =>
//                 {
//                     return new Mock<StatusServerFilteringStep>();
//                 });
//             // foreach (var step in steps)
//             // {
//             //     _serviceProviderMock
//             //         .Setup(x => x.GetService(step.Object.GetType()))
//             //         .Returns(step.Object);
//             // }
//         }
//
//         private void VerifyStepCreated<TStep>(Times times) where TStep : IRadiusPipelineStep
//         {
//             _serviceProviderMock.Verify(
//                 x => x.GetRequiredService<TStep>(),
//                 times);
//         }
//     }
// }