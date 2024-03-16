using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using MultiFactor.Radius.Adapter.Configuration.Core;
using MultiFactor.Radius.Adapter.Core.Pipeline;
using MultiFactor.Radius.Adapter.Server;
using MultiFactor.Radius.Adapter.Server.Context;
using MultiFactor.Radius.Adapter.Server.Pipeline;
using MultiFactor.Radius.Adapter.Tests.Fixtures;
using MultiFactor.Radius.Adapter.Tests.Fixtures.ConfigLoading;
using MultiFactor.Radius.Adapter.Tests.Fixtures.Radius;

namespace MultiFactor.Radius.Adapter.Tests.Pipeline
{
    [Trait("Category", "Pipeline")]
    public class Bypass2FaMidlewareTests
    {
        [Fact]
        public async Task Invoke_Bypass2IsTrue_ShouldInvokePostProcessor()
        {
            var postProcessor = new Mock<IRadiusRequestPostProcessor>();

            var host = TestHostFactory.CreateHost(builder =>
            {
                builder.Services.Configure<TestConfigProviderOptions>(x =>
                {
                    x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-single.config");
                });

                builder.Services.RemoveService<IRadiusRequestPostProcessor>().AddSingleton(postProcessor.Object);
            });

            var config = host.Service<IServiceConfiguration>();
            var responseSender = new Mock<IRadiusResponseSender>();
            var context = new RadiusContext(config.Clients[0], responseSender.Object, new Mock<IServiceProvider>().Object)
            {
                RequestPacket = RadiusPacketFactory.AccessRequest(),
                Bypass2Fa = true
            };

            var nextDelegate = new Mock<RadiusRequestDelegate>();

            var middleware = host.Service<Bypass2FaMidleware>();
            await middleware.InvokeAsync(context, nextDelegate.Object);

            nextDelegate.Verify(q => q.Invoke(It.Is<RadiusContext>(x => x == context)), Times.Never);
            postProcessor.Verify(v => v.InvokeAsync(It.Is<RadiusContext>(x => x == context)), Times.Once);
        }
        
        [Fact]
        public async Task Invoke_Bypass2IsFalse_ShouldInvokeNextDelegate()
        {
            var postProcessor = new Mock<IRadiusRequestPostProcessor>();

            var host = TestHostFactory.CreateHost(builder =>
            {
                builder.Services.Configure<TestConfigProviderOptions>(x =>
                {
                    x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-single.config");
                });

                builder.Services.RemoveService<IRadiusRequestPostProcessor>().AddSingleton(postProcessor.Object);
            });

            var config = host.Service<IServiceConfiguration>();
            var responseSender = new Mock<IRadiusResponseSender>();
            var context = new RadiusContext(config.Clients[0], responseSender.Object, new Mock<IServiceProvider>().Object)
            {
                RequestPacket = RadiusPacketFactory.AccessRequest(),
                Bypass2Fa = false
            };

            var nextDelegate = new Mock<RadiusRequestDelegate>();

            var middleware = host.Service<Bypass2FaMidleware>();
            await middleware.InvokeAsync(context, nextDelegate.Object);

            nextDelegate.Verify(q => q.Invoke(It.Is<RadiusContext>(x => x == context)), Times.Once);
            postProcessor.Verify(v => v.InvokeAsync(It.Is<RadiusContext>(x => x == context)), Times.Never);
        }

        [Fact]
        public async Task Invoke_Bypass2IsTrue_AuthStateShouldBeAccept()
        {
            var postProcessor = new Mock<IRadiusRequestPostProcessor>();

            var host = TestHostFactory.CreateHost(builder =>
            {
                builder.Services.Configure<TestConfigProviderOptions>(x =>
                {
                    x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-single.config");
                });

                builder.Services.RemoveService<IRadiusRequestPostProcessor>().AddSingleton(postProcessor.Object);
            });

            var config = host.Service<IServiceConfiguration>();
            var responseSender = new Mock<IRadiusResponseSender>();
            var context = new RadiusContext(config.Clients[0], responseSender.Object, new Mock<IServiceProvider>().Object)
            {
                RequestPacket = RadiusPacketFactory.AccessRequest(),
                Bypass2Fa = true
            };

            var middleware = host.Service<Bypass2FaMidleware>();
            await middleware.InvokeAsync(context, new Mock<RadiusRequestDelegate>().Object);

            context.ResponseCode.Should().Be(Core.Radius.PacketCode.AccessAccept);
            context.AuthenticationState.SecondFactor.Should().Be(AuthenticationCode.Accept);
        }

        [Fact]
        public async Task Invoke_Bypass2IsFalse_AuthStateShouldBeAwaiting()
        {
            var postProcessor = new Mock<IRadiusRequestPostProcessor>();

            var host = TestHostFactory.CreateHost(builder =>
            {
                builder.Services.Configure<TestConfigProviderOptions>(x =>
                {
                    x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-single.config");
                });

                builder.Services.RemoveService<IRadiusRequestPostProcessor>().AddSingleton(postProcessor.Object);
            });

            var config = host.Service<IServiceConfiguration>();
            var responseSender = new Mock<IRadiusResponseSender>();
            var context = new RadiusContext(config.Clients[0], responseSender.Object, new Mock<IServiceProvider>().Object)
            {
                RequestPacket = RadiusPacketFactory.AccessRequest(),
                Bypass2Fa = false
            };

            var middleware = host.Service<Bypass2FaMidleware>();
            await middleware.InvokeAsync(context, new Mock<RadiusRequestDelegate>().Object);

            context.ResponseCode.Should().Be(Core.Radius.PacketCode.AccessReject);
            context.AuthenticationState.SecondFactor.Should().Be(AuthenticationCode.Awaiting);
        }
    }
}
