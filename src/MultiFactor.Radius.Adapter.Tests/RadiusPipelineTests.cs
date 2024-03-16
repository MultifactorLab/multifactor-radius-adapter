using Moq;
using MultiFactor.Radius.Adapter.Configuration.Core;
using MultiFactor.Radius.Adapter.Core.Pipeline;
using MultiFactor.Radius.Adapter.Server.Context;
using MultiFactor.Radius.Adapter.Server.Pipeline;
using MultiFactor.Radius.Adapter.Server;
using MultiFactor.Radius.Adapter.Tests.Fixtures.Radius;
using MultiFactor.Radius.Adapter.Tests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using MultiFactor.Radius.Adapter.Tests.Fixtures.ConfigLoading;

namespace MultiFactor.Radius.Adapter.Tests
{
    public class RadiusPipelineTests
    {
        [Fact]
        public async Task InvokePipeline_ShouldInvokePostProcessor()
        {
            var postProcessor = new Mock<IRadiusRequestPostProcessor>();

            var host = TestHostFactory.CreateHost(builder =>
            {
                builder.Services.Configure<TestConfigProviderOptions>(x =>
                {
                    x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-single.config");
                });

                builder.Services.ReplaceService(postProcessor.Object);

                builder.Services.RemoveService<IRadiusPipeline>();
                builder.Services.AddSingleton<RadiusPipeline>();
                builder.Services.AddSingleton<IRadiusPipeline>(prov => prov.GetRequiredService<RadiusPipeline>());
            });

            var config = host.Service<IServiceConfiguration>();
            var responseSender = new Mock<IRadiusResponseSender>();
            var context = new RadiusContext(config.Clients[0], responseSender.Object, new Mock<IServiceProvider>().Object)
            {
                RequestPacket = RadiusPacketFactory.AccessRequest(),
                Bypass2Fa = true
            };

            var pipeline = host.Service<RadiusPipeline>();

            await pipeline.InvokeAsync(context);

            postProcessor.Verify(v => v.InvokeAsync(It.Is<RadiusContext>(x => x == context)), Times.Once);
        }
    }
}
