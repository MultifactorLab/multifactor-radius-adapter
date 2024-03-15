using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using MultiFactor.Radius.Adapter.Configuration;
using MultiFactor.Radius.Adapter.Configuration.Core;
using MultiFactor.Radius.Adapter.Core;
using MultiFactor.Radius.Adapter.Core.Pipeline;
using MultiFactor.Radius.Adapter.Core.Radius;
using MultiFactor.Radius.Adapter.Server;
using MultiFactor.Radius.Adapter.Server.Context;
using MultiFactor.Radius.Adapter.Server.FirstAuthFactorProcessing;
using MultiFactor.Radius.Adapter.Server.Pipeline;
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
            var host = TestHostFactory.CreateHost(services =>
            {
                services.Configure<TestConfigProviderOptions>(x =>
                {
                    x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-single.config");
                });

                var processor = new Mock<IFirstAuthFactorProcessor>();
                processor.Setup(x => x.ProcessFirstAuthFactorAsync(It.IsAny<RadiusContext>())).ReturnsAsync(PacketCode.AccessAccept);

                var fafpProv = new Mock<IFirstAuthFactorProcessorProvider>();
                fafpProv.Setup(x => x.GetProcessor(It.IsAny<AuthenticationSource>())).Returns(processor.Object);

                services.ReplaceService(fafpProv.Object);
            });

            var config = host.Services.GetRequiredService<IServiceConfiguration>();
            var responseSender = new Mock<IRadiusResponseSender>();
            var context = new RadiusContext(config.Clients[0], responseSender.Object, new Mock<IServiceProvider>().Object)
            {
                RequestPacket = RadiusPacketFactory.AccessRequest()
            };

            var nextDelegate = new Mock<RadiusRequestDelegate>();

            var middleware = host.Services.GetRequiredService<FirstFactorAuthenticationMiddleware>();
            await middleware.InvokeAsync(context, nextDelegate.Object);

            nextDelegate.Verify(q => q.Invoke(It.Is<RadiusContext>(x => x == context)), Times.Once);
        }
        
        [Fact]
        public async Task Invoke_FirstFactorReturnsNonAccept_ShouldInvokePostProcessor()
        {
            var postProcessor = new Mock<IRadiusRequestPostProcessor>();

            var host = TestHostFactory.CreateHost(services =>
            {
                services.Configure<TestConfigProviderOptions>(x =>
                {
                    x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-single.config");
                });

                var processor = new Mock<IFirstAuthFactorProcessor>();
                processor.Setup(x => x.ProcessFirstAuthFactorAsync(It.IsAny<RadiusContext>())).ReturnsAsync(PacketCode.AccessReject);

                var fafpProv = new Mock<IFirstAuthFactorProcessorProvider>();
                fafpProv.Setup(x => x.GetProcessor(It.IsAny<AuthenticationSource>())).Returns(processor.Object);

                services.ReplaceService(fafpProv.Object);
                services.ReplaceService(postProcessor.Object);
            });

            var config = host.Services.GetRequiredService<IServiceConfiguration>();
            var responseSender = new Mock<IRadiusResponseSender>();
            var context = new RadiusContext(config.Clients[0], responseSender.Object, new Mock<IServiceProvider>().Object)
            {
                RequestPacket = RadiusPacketFactory.AccessRequest()
            };

            var nextDelegate = new Mock<RadiusRequestDelegate>();

            var middleware = host.Services.GetRequiredService<FirstFactorAuthenticationMiddleware>();
            await middleware.InvokeAsync(context, nextDelegate.Object);

            nextDelegate.Verify(v => v.Invoke(It.Is<RadiusContext>(x => x == context)), Times.Never);
            postProcessor.Verify(v => v.InvokeAsync(It.Is<RadiusContext>(x => x == context)), Times.Once);
        }
        
        [Fact]
        public async Task Invoke_FirstFactorReturnsNonAccept_ShouldSetRejectState()
        {
            var postProcessor = new Mock<IRadiusRequestPostProcessor>();

            var host = TestHostFactory.CreateHost(services =>
            {
                services.Configure<TestConfigProviderOptions>(x =>
                {
                    x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-single.config");
                });

                var processor = new Mock<IFirstAuthFactorProcessor>();
                processor.Setup(x => x.ProcessFirstAuthFactorAsync(It.IsAny<RadiusContext>())).ReturnsAsync(PacketCode.AccessReject);

                var fafpProv = new Mock<IFirstAuthFactorProcessorProvider>();
                fafpProv.Setup(x => x.GetProcessor(It.IsAny<AuthenticationSource>())).Returns(processor.Object);

                services.RemoveService<IFirstAuthFactorProcessorProvider>().AddSingleton(fafpProv.Object);
                services.RemoveService<IRadiusRequestPostProcessor>().AddSingleton(postProcessor.Object);
            });

            var config = host.Services.GetRequiredService<IServiceConfiguration>();
            var responseSender = new Mock<IRadiusResponseSender>();
            var context = new RadiusContext(config.Clients[0], responseSender.Object, new Mock<IServiceProvider>().Object)
            {
                RequestPacket = RadiusPacketFactory.AccessRequest()
            };

            var middleware = host.Services.GetRequiredService<FirstFactorAuthenticationMiddleware>();
            await middleware.InvokeAsync(context, new Mock<RadiusRequestDelegate>().Object);

            context.ResponseCode.Should().Be(PacketCode.AccessReject);
            context.AuthenticationState.FirstFactor.Should().Be(AuthenticationCode.Reject);
        }
        
        [Fact]
        public async Task Invoke_FirstFactorReturnsAccept_ShouldSetAcceptState()
        {
            var postProcessor = new Mock<IRadiusRequestPostProcessor>();

            var host = TestHostFactory.CreateHost(services =>
            {
                services.Configure<TestConfigProviderOptions>(x =>
                {
                    x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-single.config");
                });

                var processor = new Mock<IFirstAuthFactorProcessor>();
                processor.Setup(x => x.ProcessFirstAuthFactorAsync(It.IsAny<RadiusContext>())).ReturnsAsync(PacketCode.AccessAccept);

                var fafpProv = new Mock<IFirstAuthFactorProcessorProvider>();
                fafpProv.Setup(x => x.GetProcessor(It.IsAny<AuthenticationSource>())).Returns(processor.Object);

                services.RemoveService<IFirstAuthFactorProcessorProvider>().AddSingleton(fafpProv.Object);
                services.RemoveService<IRadiusRequestPostProcessor>().AddSingleton(postProcessor.Object);
            });

            var config = host.Services.GetRequiredService<IServiceConfiguration>();
            var responseSender = new Mock<IRadiusResponseSender>();
            var context = new RadiusContext(config.Clients[0], responseSender.Object, new Mock<IServiceProvider>().Object)
            {
                RequestPacket = RadiusPacketFactory.AccessRequest()
            };

            var middleware = host.Services.GetRequiredService<FirstFactorAuthenticationMiddleware>();
            await middleware.InvokeAsync(context, new Mock<RadiusRequestDelegate>().Object);

            context.AuthenticationState.FirstFactor.Should().Be(AuthenticationCode.Accept);
            context.AuthenticationState.FirstFactor.Should().Be(AuthenticationCode.Accept);
        }
    }
}
