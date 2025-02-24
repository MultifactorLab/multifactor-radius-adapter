﻿using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using MultiFactor.Radius.Adapter.Core.Framework.Context;
using MultiFactor.Radius.Adapter.Core.Framework.Pipeline;
using MultiFactor.Radius.Adapter.Core.Radius;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration.ClientLevel;
using MultiFactor.Radius.Adapter.Server.Pipeline.AccessChallenge;
using MultiFactor.Radius.Adapter.Server.Pipeline.FirstFactorAuthentication;
using MultiFactor.Radius.Adapter.Server.Pipeline.FirstFactorAuthentication.Processing;
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
    }
}
