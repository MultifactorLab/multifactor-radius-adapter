using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using MultiFactor.Radius.Adapter.Configuration;
using MultiFactor.Radius.Adapter.Core.Radius;
using MultiFactor.Radius.Adapter.Framework.Context;
using MultiFactor.Radius.Adapter.Framework.Pipeline;
using MultiFactor.Radius.Adapter.Server.Pipeline.FirstFactorAuthentication;
using MultiFactor.Radius.Adapter.Server.Pipeline.FirstFactorAuthentication.FirstAuthFactorProcessing;
using MultiFactor.Radius.Adapter.Tests.Fixtures;
using MultiFactor.Radius.Adapter.Tests.Fixtures.ConfigLoading;
using MultiFactor.Radius.Adapter.Tests.Fixtures.Radius;

namespace MultiFactor.Radius.Adapter.Tests.Pipeline
{
    [Trait("Category", "Pipeline")]
    public class FirstFactorAuthenticationMiddlewareTests
    {
        [Fact]
        public async Task Invoke_FirstFactorReturnsAccept_ShouldInvokeNext()
        {
            var host = TestHostFactory.CreateHost(builder =>
            {
                builder.Services.Configure<TestConfigProviderOptions>(x =>
                {
                    x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-single.config");
                });

                var processor = new Mock<IFirstAuthFactorProcessor>();
                processor.Setup(x => x.ProcessFirstAuthFactorAsync(It.IsAny<RadiusContext>())).ReturnsAsync(PacketCode.AccessAccept);

                var fafpProv = new Mock<IFirstAuthFactorProcessorProvider>();
                fafpProv.Setup(x => x.GetProcessor(It.IsAny<AuthenticationSource>())).Returns(processor.Object);
                builder.Services.ReplaceService(fafpProv.Object);

                builder.UseMiddleware<FirstFactorAuthenticationMiddleware>();
            });

            var context = host.CreateContext(RadiusPacketFactory.AccessRequest());
            var nextDelegate = new Mock<RadiusRequestDelegate>();

            var middleware = host.Service<FirstFactorAuthenticationMiddleware>();
            await middleware.InvokeAsync(context, nextDelegate.Object);

            nextDelegate.Verify(q => q.Invoke(It.Is<RadiusContext>(x => x == context)), Times.Once);
        }
        
        [Fact]
        public async Task Invoke_FirstFactorReturnsNonAccept_ShouldNotInvokeNext()
        {

            var host = TestHostFactory.CreateHost(builder =>
            {
                builder.Services.Configure<TestConfigProviderOptions>(x =>
                {
                    x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-single.config");
                });

                var processor = new Mock<IFirstAuthFactorProcessor>();
                processor.Setup(x => x.ProcessFirstAuthFactorAsync(It.IsAny<RadiusContext>())).ReturnsAsync(PacketCode.AccessReject);

                var fafpProv = new Mock<IFirstAuthFactorProcessorProvider>();
                fafpProv.Setup(x => x.GetProcessor(It.IsAny<AuthenticationSource>())).Returns(processor.Object);

                builder.Services.ReplaceService(fafpProv.Object);
                builder.UseMiddleware<FirstFactorAuthenticationMiddleware>();
            });

            var context = host.CreateContext(RadiusPacketFactory.AccessRequest());
            var nextDelegate = new Mock<RadiusRequestDelegate>();

            var middleware = host.Service<FirstFactorAuthenticationMiddleware>();
            await middleware.InvokeAsync(context, nextDelegate.Object);

            nextDelegate.Verify(v => v.Invoke(It.Is<RadiusContext>(x => x == context)), Times.Never);
        }
        
        [Fact]
        public async Task Invoke_FirstFactorReturnsNonAccept_ShouldSetRejectState()
        {
            var postProcessor = new Mock<IRadiusRequestPostProcessor>();

            var host = TestHostFactory.CreateHost(builder =>
            {
                builder.Services.Configure<TestConfigProviderOptions>(x =>
                {
                    x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-single.config");
                });

                var processor = new Mock<IFirstAuthFactorProcessor>();
                processor.Setup(x => x.ProcessFirstAuthFactorAsync(It.IsAny<RadiusContext>())).ReturnsAsync(PacketCode.AccessReject);

                var fafpProv = new Mock<IFirstAuthFactorProcessorProvider>();
                fafpProv.Setup(x => x.GetProcessor(It.IsAny<AuthenticationSource>())).Returns(processor.Object);

                builder.Services.RemoveService<IFirstAuthFactorProcessorProvider>().AddSingleton(fafpProv.Object);
                builder.UseMiddleware<FirstFactorAuthenticationMiddleware>();
            });

            var context = host.CreateContext(RadiusPacketFactory.AccessRequest());

            var middleware = host.Service<FirstFactorAuthenticationMiddleware>();
            await middleware.InvokeAsync(context, new Mock<RadiusRequestDelegate>().Object);

            context.AuthenticationState.FirstFactor.Should().Be(AuthenticationCode.Reject);
        }
        
        [Fact]
        public async Task Invoke_FirstFactorReturnsAccept_ShouldSetAcceptState()
        {
            var postProcessor = new Mock<IRadiusRequestPostProcessor>();

            var host = TestHostFactory.CreateHost(builder =>
            {
                builder.Services.Configure<TestConfigProviderOptions>(x =>
                {
                    x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-single.config");
                });

                var processor = new Mock<IFirstAuthFactorProcessor>();
                processor.Setup(x => x.ProcessFirstAuthFactorAsync(It.IsAny<RadiusContext>())).ReturnsAsync(PacketCode.AccessAccept);

                var fafpProv = new Mock<IFirstAuthFactorProcessorProvider>();
                fafpProv.Setup(x => x.GetProcessor(It.IsAny<AuthenticationSource>())).Returns(processor.Object);

                builder.Services.RemoveService<IFirstAuthFactorProcessorProvider>().AddSingleton(fafpProv.Object);
                builder.UseMiddleware<FirstFactorAuthenticationMiddleware>();
            });

            var context = host.CreateContext(RadiusPacketFactory.AccessRequest());

            var middleware = host.Service<FirstFactorAuthenticationMiddleware>();
            await middleware.InvokeAsync(context, new Mock<RadiusRequestDelegate>().Object);

            context.AuthenticationState.FirstFactor.Should().Be(AuthenticationCode.Accept);
        }
    }
}
