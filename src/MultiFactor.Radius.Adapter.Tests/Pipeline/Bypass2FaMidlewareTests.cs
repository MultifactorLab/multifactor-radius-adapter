using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using MultiFactor.Radius.Adapter.Framework.Context;
using MultiFactor.Radius.Adapter.Framework.Pipeline;
using MultiFactor.Radius.Adapter.Server.Pipeline.Bypass2Fa;
using MultiFactor.Radius.Adapter.Tests.Fixtures;
using MultiFactor.Radius.Adapter.Tests.Fixtures.ConfigLoading;
using MultiFactor.Radius.Adapter.Tests.Fixtures.Radius;

namespace MultiFactor.Radius.Adapter.Tests.Pipeline
{
    [Trait("Category", "Pipeline")]
    public class Bypass2FaMidlewareTests
    {
        [Fact]
        public async Task Invoke_Bypass2IsTrue_ShouldNotInvokeNext()
        {

            var host = TestHostFactory.CreateHost(builder =>
            {
                builder.Services.Configure<TestConfigProviderOptions>(x =>
                {
                    x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-single.config");
                });
                builder.UseMiddleware<Bypass2FaMidleware>();
            });

            var context = host.CreateContext(RadiusPacketFactory.AccessRequest(), setupContext: x =>
            {
                x.Bypass2Fa = true;
            });

            var nextDelegate = new Mock<RadiusRequestDelegate>();

            var middleware = host.Service<Bypass2FaMidleware>();
            await middleware.InvokeAsync(context, nextDelegate.Object);

            nextDelegate.Verify(q => q.Invoke(It.Is<RadiusContext>(x => x == context)), Times.Never);
        }
        
        [Fact]
        public async Task Invoke_Bypass2IsFalse_ShouldInvokeNextDelegate()
        {
            var host = TestHostFactory.CreateHost(builder =>
            {
                builder.Services.Configure<TestConfigProviderOptions>(x =>
                {
                    x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-single.config");
                });
                builder.UseMiddleware<Bypass2FaMidleware>();
            });

            var context = host.CreateContext(RadiusPacketFactory.AccessRequest(), setupContext: x =>
            {
                x.Bypass2Fa = false;
            });

            var nextDelegate = new Mock<RadiusRequestDelegate>();

            var middleware = host.Service<Bypass2FaMidleware>();
            await middleware.InvokeAsync(context, nextDelegate.Object);

            nextDelegate.Verify(q => q.Invoke(It.Is<RadiusContext>(x => x == context)), Times.Once);
        }

        [Fact]
        public async Task Invoke_Bypass2IsTrue_AuthStateShouldBeAccept()
        {
            var host = TestHostFactory.CreateHost(builder =>
            {
                builder.Services.Configure<TestConfigProviderOptions>(x =>
                {
                    x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-single.config");
                });
                builder.UseMiddleware<Bypass2FaMidleware>();
            });

            var context = host.CreateContext(RadiusPacketFactory.AccessRequest(), setupContext: x =>
            {
                x.Bypass2Fa = true;
            });

            await host.InvokePipeline(context);

            context.ResponseCode.Should().Be(Core.Radius.PacketCode.AccessAccept);
            context.Authentication.SecondFactor.Should().Be(AuthenticationCode.Accept);
        }

        [Fact]
        public async Task Invoke_Bypass2IsFalse_AuthStateShouldBeAwaiting()
        {
            var host = TestHostFactory.CreateHost(builder =>
            {
                builder.Services.Configure<TestConfigProviderOptions>(x =>
                {
                    x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-single.config");
                });
                builder.UseMiddleware<Bypass2FaMidleware>();
            });

            var context = host.CreateContext(RadiusPacketFactory.AccessRequest(), setupContext: x =>
            {
                x.Bypass2Fa = false;
            });

            await host.InvokePipeline(context);

            context.ResponseCode.Should().Be(Core.Radius.PacketCode.AccessReject);
            context.Authentication.SecondFactor.Should().Be(AuthenticationCode.Awaiting);
        }
    }
}
