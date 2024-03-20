using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using MultiFactor.Radius.Adapter.Configuration.Core;
using MultiFactor.Radius.Adapter.Core.Radius;
using MultiFactor.Radius.Adapter.Framework.Context;
using MultiFactor.Radius.Adapter.Framework.Pipeline;
using MultiFactor.Radius.Adapter.Server;
using MultiFactor.Radius.Adapter.Server.Pipeline.StatusServer;
using MultiFactor.Radius.Adapter.Tests.Fixtures;
using MultiFactor.Radius.Adapter.Tests.Fixtures.ConfigLoading;
using MultiFactor.Radius.Adapter.Tests.Fixtures.Radius;

namespace MultiFactor.Radius.Adapter.Tests.Pipeline
{
    [Trait("Category", "Pipeline")]
    public class StatusServerMiddlewareTests
    {
        [Fact]
        public async Task Invoke_StatusServerRequest_ShoulSetReplyMessage()
        {
            var expectedTs = TimeSpan.FromMinutes(90);
            var expectedVer = "8.8.8";

            var host = TestHostFactory.CreateHost(builder =>
            {
                builder.Services.Configure<TestConfigProviderOptions>(x =>
                {
                    x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-single.config");
                });

                builder.UseMiddleware<StatusServerMiddleware>();

                var serverInfo = new Mock<IServerInfo>();
                serverInfo.Setup(x => x.GetUptime()).Returns(expectedTs);
                serverInfo.Setup(x => x.GetVersion()).Returns(expectedVer);
                builder.Services.ReplaceService(serverInfo.Object);

                builder.Services.ReplaceService(new Mock<IRadiusResponseSender>().Object);
            });

            var context = host.CreateContext(RadiusPacketFactory.StatusServer());

            var pipeline = host.Service<RadiusPipeline>();
            await pipeline.InvokeAsync(context);

            context.AuthenticationState.ToPacketCode().Should().Be(PacketCode.AccessAccept);
            context.ResponseCode.Should().Be(PacketCode.AccessAccept);

            context.ReplyMessage.Should().Be($"Server up {expectedTs.Days} days {expectedTs.ToString("hh\\:mm\\:ss")}, ver.: {expectedVer}");
        }
        
        [Fact]
        public async Task Invoke_NonStatusServerRequest_ShouldInvokeNext()
        {
            var host = TestHostFactory.CreateHost(builder =>
            {
                builder.UseMiddleware<StatusServerMiddleware>();
                builder.Services.Configure<TestConfigProviderOptions>(x =>
                {
                    x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-single.config");
                });
            });

            var context = host.CreateContext(RadiusPacketFactory.AccessRequest());
            var nextDelegate = new Mock<RadiusRequestDelegate>();

            var middleware = host.Service<StatusServerMiddleware>();
            await middleware.InvokeAsync(context, nextDelegate.Object);

            context.ReplyMessage.Should().BeNullOrEmpty();
            nextDelegate.Verify(q => q.Invoke(It.Is<RadiusContext>(x => x == context)), Times.Once);
        }
    }
}
