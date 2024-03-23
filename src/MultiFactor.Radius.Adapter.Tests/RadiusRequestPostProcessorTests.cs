using Moq;
using MultiFactor.Radius.Adapter.Tests.Fixtures.Radius;
using MultiFactor.Radius.Adapter.Tests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using MultiFactor.Radius.Adapter.Tests.Fixtures.ConfigLoading;
using MultiFactor.Radius.Adapter.Framework.Pipeline;
using MultiFactor.Radius.Adapter.Server.Pipeline.PostProcessing;
using MultiFactor.Radius.Adapter.Server;
using MultiFactor.Radius.Adapter.Configuration.Core;
using MultiFactor.Radius.Adapter.Core.Radius;
using System.Net;
using MultiFactor.Radius.Adapter.Services;

namespace MultiFactor.Radius.Adapter.Tests
{
    public class RadiusRequestPostProcessorTests
    {
        [Fact]
        public async Task AccessChallenge_ShouldSetAttrs()
        {
            var host = TestHostFactory.CreateHost(builder =>
            {
                builder.Services.Configure<TestConfigProviderOptions>(x =>
                {
                    x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-single.config");
                });

                builder.Services.RemoveService<IRadiusRequestPostProcessor>();
                builder.Services.AddSingleton<RadiusRequestPostProcessor>();
                builder.Services.AddSingleton<IRadiusRequestPostProcessor>(prov => prov.GetRequiredService<RadiusRequestPostProcessor>());

                var sender = new Mock<IRadiusResponseSender>();
                var factory = new Mock<IRadiusResponseSenderFactory>();
                factory.Setup(x => x.CreateSender(It.IsAny<IUdpClient>())).Returns(sender.Object);
                builder.Services.ReplaceService(factory.Object);
            });

            var context = host.CreateContext(RadiusPacketFactory.AccessChallenge());
            context.ResponseCode = PacketCode.AccessChallenge;
            context.ReplyMessage = "My Message";
            context.State = "My State";

            IRadiusPacket? sentPacket = null;
            var fakeSender = Mock.Get(host.Service<IRadiusResponseSenderFactory>().CreateSender(null));
            fakeSender
                .Setup(x => x.Send(It.IsAny<IRadiusPacket>(), It.IsAny<string>(), It.IsAny<IPEndPoint>(), It.IsAny<IPEndPoint>(), It.IsAny<bool>()))
                .Callback((IRadiusPacket rp, string u, IPEndPoint re, IPEndPoint pe, bool dl) =>
                {
                    sentPacket = rp;
                });
            
            var srv = host.Service<RadiusRequestPostProcessor>();
            await srv.InvokeAsync(context);

            Assert.NotNull(sentPacket);

            var reply = sentPacket.GetAttribute<string>("Reply-Message");
            var state = sentPacket.GetAttribute<string>("State");

            Assert.Equal("My Message", reply);
            Assert.Equal("My State", state);
        }
        
        [Fact]
        public async Task AccessReject_ShouldInvokeWaiter()
        {
            var waiter = new Mock<IRandomWaiter>();
            var host = TestHostFactory.CreateHost(builder =>
            {
                builder.Services.Configure<TestConfigProviderOptions>(x =>
                {
                    x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-single.config");
                });

                builder.Services.RemoveService<IRadiusRequestPostProcessor>();
                builder.Services.AddSingleton<RadiusRequestPostProcessor>();
                builder.Services.AddSingleton<IRadiusRequestPostProcessor>(prov => prov.GetRequiredService<RadiusRequestPostProcessor>());

                var factory = new Mock<IRadiusResponseSenderFactory>();
                factory.Setup(x => x.CreateSender(It.IsAny<IUdpClient>())).Returns(new Mock<IRadiusResponseSender>().Object); 
                builder.Services.ReplaceService(factory.Object);

                builder.Services.ReplaceService(waiter.Object);
            });

            var context = host.CreateContext(RadiusPacketFactory.AccessReject());
            context.ResponseCode = PacketCode.AccessReject;

            var srv = host.Service<RadiusRequestPostProcessor>();
            await srv.InvokeAsync(context);

            waiter.Verify(x => x.WaitSomeTimeAsync(), Times.Once);
        }
        
        [Fact]
        public async Task ProxyEcho_ShouldSetAttr()
        {
            var host = TestHostFactory.CreateHost(builder =>
            {
                builder.Services.Configure<TestConfigProviderOptions>(x =>
                {
                    x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-single.config");
                });

                builder.Services.RemoveService<IRadiusRequestPostProcessor>();
                builder.Services.AddSingleton<RadiusRequestPostProcessor>();
                builder.Services.AddSingleton<IRadiusRequestPostProcessor>(prov => prov.GetRequiredService<RadiusRequestPostProcessor>());

                var factory = new Mock<IRadiusResponseSenderFactory>();
                factory.Setup(x => x.CreateSender(It.IsAny<IUdpClient>())).Returns(new Mock<IRadiusResponseSender>().Object); 
                builder.Services.ReplaceService(factory.Object);
            });

            var context = host.CreateContext(RadiusPacketFactory.AccessRequest());
            context.ResponseCode = PacketCode.AccessAccept;
            context.RequestPacket.AddAttribute("Proxy-State", "VAL");

            IRadiusPacket? sentPacket = null;
            var fakeSender = Mock.Get(host.Service<IRadiusResponseSenderFactory>().CreateSender(null));
            fakeSender
                .Setup(x => x.Send(It.IsAny<IRadiusPacket>(), It.IsAny<string>(), It.IsAny<IPEndPoint>(), It.IsAny<IPEndPoint>(), It.IsAny<bool>()))
                .Callback((IRadiusPacket rp, string u, IPEndPoint re, IPEndPoint pe, bool dl) =>
                {
                    sentPacket = rp;
                });

            var srv = host.Service<RadiusRequestPostProcessor>();
            await srv.InvokeAsync(context);

            Assert.NotNull(sentPacket);

            var proxy = sentPacket.GetAttribute<string>("Proxy-State");

            Assert.Equal("VAL", proxy);
        }
    }
}
