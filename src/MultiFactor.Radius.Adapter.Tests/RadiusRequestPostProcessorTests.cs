using Moq;
using MultiFactor.Radius.Adapter.Tests.Fixtures.Radius;
using MultiFactor.Radius.Adapter.Tests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using MultiFactor.Radius.Adapter.Tests.Fixtures.ConfigLoading;
using MultiFactor.Radius.Adapter.Framework.Pipeline;
using MultiFactor.Radius.Adapter.Server.Pipeline.PostProcessing;
using MultiFactor.Radius.Adapter.Server;
using MultiFactor.Radius.Adapter.Core.Radius;
using System.Net;

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
                builder.Services.ReplaceService(sender.Object);
            });

            var context = host.CreateContext(RadiusPacketFactory.AccessChallenge());
            context.SetReplyMessage("My Message");
            context.SetMessageState("My State");

            IRadiusPacket? sentPacket = null;
            var fakeSender = Mock.Get(host.Service<IRadiusResponseSender>());
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

                var sender = new Mock<IRadiusResponseSender>();
                builder.Services.ReplaceService(sender.Object);
            });

            var context = host.CreateContext(RadiusPacketFactory.AccessRequest());
            context.Authentication.Accept();
            context.RequestPacket.AddAttribute("Proxy-State", "VAL");

            IRadiusPacket? sentPacket = null;
            var fakeSender = Mock.Get(host.Service<IRadiusResponseSender>());
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
