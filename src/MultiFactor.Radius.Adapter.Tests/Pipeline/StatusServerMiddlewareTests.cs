using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using MultiFactor.Radius.Adapter.Configuration.Core;
using MultiFactor.Radius.Adapter.Core;
using MultiFactor.Radius.Adapter.Core.Pipeline;
using MultiFactor.Radius.Adapter.Core.Radius;
using MultiFactor.Radius.Adapter.Server;
using MultiFactor.Radius.Adapter.Server.Pipeline;
using MultiFactor.Radius.Adapter.Tests.Fixtures;
using MultiFactor.Radius.Adapter.Tests.Fixtures.ConfigLoading;
using MultiFactor.Radius.Adapter.Tests.Fixtures.Radius;

namespace MultiFactor.Radius.Adapter.Tests.Pipeline
{
    public class StatusServerMiddlewareTests
    {
        [Fact]
        public async Task Invoke_StatusServerRequest_ShoulSetReplyMessage()
        {
            var expectedTs = TimeSpan.FromMinutes(90);
            var expectedVer = "8.8.8";

            var host = TestHostFactory.CreateHost(services =>
            {
                services.RemoveService<IRootConfigurationProvider>().AddSingleton<IRootConfigurationProvider, TestRootConfigProvider>();
                services.RemoveService<IClientConfigurationsProvider>().AddSingleton<IClientConfigurationsProvider, TestClientConfigsProvider>();
                services.Configure<TestConfigProviderOptions>(x =>
                {
                    x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-single.config");
                });

                var serverInfo = new Mock<IServerInfo>();
                serverInfo.Setup(x => x.GetUptime()).Returns(expectedTs);
                serverInfo.Setup(x => x.GetVersion()).Returns(expectedVer);
                services.RemoveService<IServerInfo>().AddSingleton<IServerInfo>(serverInfo.Object);
            });

            var config = host.Services.GetRequiredService<IServiceConfiguration>();
            var responseSender = new Mock<IRadiusResponseSender>();
            var context = new RadiusContext(config.Clients[0], responseSender.Object, new Mock<IServiceProvider>().Object)
            {
                RequestPacket = RadiusPacketFactory.StatusServer()
            };

            var nextDelegate = new Mock<RadiusRequestDelegate>();

            var middleware = host.Services.GetRequiredService<StatusServerMiddleware>();
            await middleware.InvokeAsync(context, nextDelegate.Object);

            context.ResponseCode.Should().Be(PacketCode.AccessAccept);
            context.ReplyMessage.Should().Be($"Server up {expectedTs.Days} days {expectedTs.ToString("hh\\:mm\\:ss")}, ver.: {expectedVer}");
            nextDelegate.Verify(q => q.Invoke(It.IsAny<RadiusContext>()), Times.Never);
        }
        
        [Fact]
        public async Task Invoke_NonStatusServerRequest_ShouldInvokeNext()
        {
            var host = TestHostFactory.CreateHost(services =>
            {
                services.RemoveService<IRootConfigurationProvider>().AddSingleton<IRootConfigurationProvider, TestRootConfigProvider>();
                services.RemoveService<IClientConfigurationsProvider>().AddSingleton<IClientConfigurationsProvider, TestClientConfigsProvider>();
                services.Configure<TestConfigProviderOptions>(x =>
                {
                    x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-single.config");
                });
            });

            var config = host.Services.GetRequiredService<IServiceConfiguration>();
            var responseSender = new Mock<IRadiusResponseSender>();
            var context = new RadiusContext(config.Clients[0], responseSender.Object, new Mock<IServiceProvider>().Object)
            {
                RequestPacket = RadiusPacketFactory.AccessRequest()
            };

            var nextDelegate = new Mock<RadiusRequestDelegate>();

            var middleware = host.Services.GetRequiredService<StatusServerMiddleware>();
            await middleware.InvokeAsync(context, nextDelegate.Object);

            context.ReplyMessage.Should().BeNullOrEmpty();
            nextDelegate.Verify(q => q.Invoke(It.Is<RadiusContext>(x => x == context)), Times.Once);
        }
    }
}
