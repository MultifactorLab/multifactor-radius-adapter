using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using MultiFactor.Radius.Adapter.Core.Framework.Context;
using MultiFactor.Radius.Adapter.Core.Framework.Pipeline;
using MultiFactor.Radius.Adapter.Core.Radius;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration.ClientLevel;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration.Features.PreAuthModeFeature;
using MultiFactor.Radius.Adapter.Server.Pipeline.AccessChallenge;
using MultiFactor.Radius.Adapter.Server.Pipeline.FirstFactorAuthentication;
using MultiFactor.Radius.Adapter.Server.Pipeline.FirstFactorAuthentication.Processing;
using MultiFactor.Radius.Adapter.Services.Ldap.MembershipVerification;
using MultiFactor.Radius.Adapter.Tests.Fixtures;
using MultiFactor.Radius.Adapter.Tests.Fixtures.ConfigLoading;
using MultiFactor.Radius.Adapter.Tests.Fixtures.Radius;

namespace MultiFactor.Radius.Adapter.Tests.Pipeline
{
    [Trait("Category", "Pipeline")]
    public class FirstFactorAuthenticationMiddlewareTests
    {    
        [Fact]
        public async Task Invoke_FirstFactorReturnsNonAccept_ShouldSetRejectState()
        {
            var host = TestHostFactory.CreateHost(builder =>
            {
                builder.Services.Configure<TestConfigProviderOptions>(x =>
                {
                    x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-single.config");
                });

                var processor = new Mock<IFirstFactorAuthenticationProcessor>();
                processor.Setup(x => x.ProcessFirstAuthFactorAsync(It.IsAny<RadiusContext>())).ReturnsAsync(PacketCode.AccessReject);

                var fafpProv = new Mock<IFirstFactorAuthenticationProcessorProvider>();
                fafpProv.Setup(x => x.GetProcessor(It.IsAny<AuthenticationSource>())).Returns(processor.Object);

                builder.Services.RemoveService<IFirstFactorAuthenticationProcessorProvider>().AddSingleton(fafpProv.Object);
                builder.UseMiddleware<FirstFactorAuthenticationMiddleware>();
            });

            var context = host.CreateContext(RadiusPacketFactory.AccessRequest());

            var middleware = host.Service<FirstFactorAuthenticationMiddleware>();
            await middleware.InvokeAsync(context, new Mock<RadiusRequestDelegate>().Object);

            context.Authentication.FirstFactor.Should().Be(AuthenticationCode.Reject);
        }
        
        [Fact]
        public async Task Invoke_FirstFactorReturnsAccept_ShouldSetAcceptState()
        {
            var host = TestHostFactory.CreateHost(builder =>
            {
                builder.Services.Configure<TestConfigProviderOptions>(x =>
                {
                    x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-single.config");
                });

                var processor = new Mock<IFirstFactorAuthenticationProcessor>();
                processor.Setup(x => x.ProcessFirstAuthFactorAsync(It.IsAny<RadiusContext>())).ReturnsAsync(PacketCode.AccessAccept);

                var fafpProv = new Mock<IFirstFactorAuthenticationProcessorProvider>();
                fafpProv.Setup(x => x.GetProcessor(It.IsAny<AuthenticationSource>())).Returns(processor.Object);

                builder.Services.RemoveService<IFirstFactorAuthenticationProcessorProvider>().AddSingleton(fafpProv.Object);
                builder.UseMiddleware<FirstFactorAuthenticationMiddleware>();
            });

            var context = host.CreateContext(RadiusPacketFactory.AccessRequest());

            var middleware = host.Service<FirstFactorAuthenticationMiddleware>();
            await middleware.InvokeAsync(context, new Mock<RadiusRequestDelegate>().Object);

            context.Authentication.FirstFactor.Should().Be(AuthenticationCode.Accept);
        }
        
        [Fact]
        public async Task Invoke_FirstFactorSourceIsNoneAndNoMembershipVerification_ShouldSetAcceptState()
        {
            var host = TestHostFactory.CreateHost(builder =>
            {
                builder.Services.Configure<TestConfigProviderOptions>(x =>
                {
                    x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-single.config");
                });

                builder.UseMiddleware<AnonymousFirstFactorAuthenticationMiddleware>();
            });

            var client = new ClientConfiguration("custom", "shared_secret", AuthenticationSource.None, "key", "secret");
            var context = host.CreateContext(RadiusPacketFactory.AccessRequest(), clientConfig: client);
            await host.InvokePipeline(context);

            Assert.Equal(AuthenticationCode.Accept, context.Authentication.FirstFactor);
        }
        
        [Fact]
        public async Task Invoke_MustChangePassword_ShouldAwaitFirstFactor()
        {
            var challengeProcessorProvider = new Mock<IChallengeProcessorProvider>();
            var challengeProcessor = new Mock<IChallengeProcessor>();
            
            var host = TestHostFactory.CreateHost(builder =>
            {
                builder.Services.Configure<TestConfigProviderOptions>(x =>
                {
                    x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-single.config");
                });

                challengeProcessorProvider.Setup(x => x.GetChallengeProcessorByType(ChallengeType.PasswordChange))
                    .Returns(challengeProcessor.Object);
                var processor = new Mock<IFirstFactorAuthenticationProcessor>();
                processor.Setup(x => x.ProcessFirstAuthFactorAsync(It.IsAny<RadiusContext>())).ReturnsAsync(PacketCode.AccessReject);

                var fafpProv = new Mock<IFirstFactorAuthenticationProcessorProvider>();
                fafpProv.Setup(x => x.GetProcessor(It.IsAny<AuthenticationSource>())).Returns(processor.Object);

                builder.Services.RemoveService<IFirstFactorAuthenticationProcessorProvider>().AddSingleton(fafpProv.Object);
                builder.Services.ReplaceService(challengeProcessorProvider.Object);
                builder.UseMiddleware<FirstFactorAuthenticationMiddleware>();
            });

            var client = new ClientConfiguration("custom", "shared_secret", AuthenticationSource.None, "key", "secret");
            var context = host.CreateContext(RadiusPacketFactory.AccessRequest(), clientConfig: client);
            context.SetMustChangePassword("must");
            
            await host.InvokePipeline(context);

            Assert.Equal(AuthenticationCode.Awaiting, context.Authentication.FirstFactor);
        }
        
        [Fact]
        public async Task Invoke_MustChangePassword_ShouldAddPasswordChallenge()
        {
            var challengeProcessorProvider = new Mock<IChallengeProcessorProvider>();
            var challengeProcessor = new Mock<IChallengeProcessor>();
            
            var host = TestHostFactory.CreateHost(builder =>
            {
                builder.Services.Configure<TestConfigProviderOptions>(x =>
                {
                    x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-single.config");
                });

                challengeProcessorProvider.Setup(x => x.GetChallengeProcessorByType(ChallengeType.PasswordChange))
                    .Returns(challengeProcessor.Object);
                var processor = new Mock<IFirstFactorAuthenticationProcessor>();
                processor.Setup(x => x.ProcessFirstAuthFactorAsync(It.IsAny<RadiusContext>())).ReturnsAsync(PacketCode.AccessReject);

                var fafpProv = new Mock<IFirstFactorAuthenticationProcessorProvider>();
                fafpProv.Setup(x => x.GetProcessor(It.IsAny<AuthenticationSource>())).Returns(processor.Object);

                builder.Services.RemoveService<IFirstFactorAuthenticationProcessorProvider>().AddSingleton(fafpProv.Object);
                builder.Services.ReplaceService(challengeProcessorProvider.Object);
                builder.UseMiddleware<FirstFactorAuthenticationMiddleware>();
            });

            var client = new ClientConfiguration("custom", "shared_secret", AuthenticationSource.None, "key", "secret");
            var context = host.CreateContext(RadiusPacketFactory.AccessRequest(), clientConfig: client);
            context.SetMustChangePassword("must");
            
            await host.InvokePipeline(context);
            
            challengeProcessorProvider.Verify(x => x.GetChallengeProcessorByType(ChallengeType.PasswordChange), Times.Once);
            challengeProcessor.Verify(x => x.AddChallengeContext(It.IsAny<RadiusContext>()), Times.Once);
        }
        
        [Fact]
        public async Task Invoke_FirstFactorSourceIsNoneAndWinLogonAccount_ShouldInvokeVerifyMembership()
        {
            var processorMock = new Mock<IMembershipProcessor>();
            processorMock
                .Setup(x => x.ProcessMembershipAsync(It.IsAny<RadiusContext>()))
                .ReturnsAsync(MembershipProcessingResult.Empty);
            var host = TestHostFactory.CreateHost(builder =>
            {
                builder.Services.Configure<TestConfigProviderOptions>(x =>
                {
                    x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-single.config");
                });
            
                builder.UseMiddleware<AnonymousFirstFactorAuthenticationMiddleware>();
                builder.InternalHostApplicationBuilder.Services.ReplaceService<IMembershipProcessor>(processorMock.Object);
            });

            var client = new Mock<IClientConfiguration>();
            client.Setup(x => x.FirstFactorAuthenticationSource).Returns(AuthenticationSource.None);
            client.Setup(x => x.ShouldLoadUserProfile).Returns(true);
            client.Setup(x => x.ShouldLoadUserGroups).Returns(true);
            client.Setup(x => x.PreAuthnMode).Returns(PreAuthModeDescriptor.Default);
            
            var packetMock = new Mock<IRadiusPacket>();
            packetMock.Setup(x => x.UserName).Returns("user");
            packetMock.Setup(x => x.TryGetUserPassword()).Returns("password");
            packetMock.Setup(x => x.AccountType).Returns(AccountType.Domain);
            
            var context = host.CreateContext(packetMock.Object, clientConfig: client.Object);
            await host.InvokePipeline(context);

            processorMock.Verify(x => x.ProcessMembershipAsync(It.IsAny<RadiusContext>()), Times.Once);
        }
        
        [Theory]
        [InlineData(AccountType.Unknown)]
        [InlineData(AccountType.Local)]
        [InlineData(AccountType.Microsoft)]
        public async Task Invoke_FirstFactorSourceIsNoneAndWinLogonAccount_ShouldNotInvokeVerifyMembership(AccountType accountType)
        {
            var processorMock = new Mock<IMembershipProcessor>();
            
            var host = TestHostFactory.CreateHost(builder =>
            {
                builder.Services.Configure<TestConfigProviderOptions>(x =>
                {
                    x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-single.config");
                });
            
                builder.UseMiddleware<AnonymousFirstFactorAuthenticationMiddleware>();
                builder.InternalHostApplicationBuilder.Services.ReplaceService<IMembershipProcessor>(processorMock.Object);
            });

            var client = new Mock<IClientConfiguration>();
            client.Setup(x => x.FirstFactorAuthenticationSource).Returns(AuthenticationSource.None);
            client.Setup(x => x.ShouldLoadUserProfile).Returns(true);
            client.Setup(x => x.ShouldLoadUserGroups).Returns(true);
            client.Setup(x => x.PreAuthnMode).Returns(PreAuthModeDescriptor.Default);
            
            var packetMock = new Mock<IRadiusPacket>();
            packetMock.Setup(x => x.UserName).Returns("user");
            packetMock.Setup(x => x.TryGetUserPassword()).Returns("password");
            packetMock.Setup(x => x.AccountType).Returns(accountType);
            
            var context = host.CreateContext(packetMock.Object, clientConfig: client.Object);
            await host.InvokePipeline(context);

            processorMock.Verify(x => x.ProcessMembershipAsync(It.IsAny<RadiusContext>()), Times.Never);
        }
    }
}
