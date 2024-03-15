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

            var host = TestHostFactory.CreateHost(services =>
            {
                services.Configure<TestConfigProviderOptions>(x =>
                {
                    x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-single.config");
                });

                services.ReplaceService(postProcessor.Object);
                services.RemoveService<IRadiusPipeline>();

                services.AddSingleton<RadiusPipeline>();
                services.AddSingleton<IRadiusPipeline>(prov => prov.GetRequiredService<RadiusPipeline>());

                RadiusRequestDelegate emptyRequestDelegate = ctx => Task.CompletedTask;
                services.ReplaceService(emptyRequestDelegate);
            });

            var config = host.Services.GetRequiredService<IServiceConfiguration>();
            var responseSender = new Mock<IRadiusResponseSender>();
            var context = new RadiusContext(config.Clients[0], responseSender.Object, new Mock<IServiceProvider>().Object)
            {
                RequestPacket = RadiusPacketFactory.AccessRequest(),
                Bypass2Fa = true
            };

            var pipeline = host.Services.GetRequiredService<RadiusPipeline>();

            await pipeline.InvokeAsync(context);

            postProcessor.Verify(v => v.InvokeAsync(It.Is<RadiusContext>(x => x == context)), Times.Once);
        }
        
        //[Fact]
        //public async Task InvokePipelineWithMiddleware_ShouldInvokePostProcessor()
        //{
        //    var postProcessor = new Mock<IRadiusRequestPostProcessor>();

        //    var host = TestHostFactory.CreateHost(services =>
        //    {
        //        services.Configure<TestConfigProviderOptions>(x =>
        //        {
        //            x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-single.config");
        //        });

        //        services.ReplaceService(postProcessor.Object);
        //        services.RemoveService<IRadiusMiddleware>();
        //        services.
        //        services.RemoveService<IRadiusPipeline>();

        //        services.AddSingleton<RadiusPipeline>();
        //        services.AddSingleton<IRadiusPipeline>(prov => prov.GetRequiredService<RadiusPipeline>());

        //        RadiusRequestDelegate emptyRequestDelegate = ctx => Task.CompletedTask;
        //        services.ReplaceService(emptyRequestDelegate);
        //    });

        //    var config = host.Services.GetRequiredService<IServiceConfiguration>();
        //    var responseSender = new Mock<IRadiusResponseSender>();
        //    var context = new RadiusContext(config.Clients[0], responseSender.Object, new Mock<IServiceProvider>().Object)
        //    {
        //        RequestPacket = RadiusPacketFactory.AccessRequest(),
        //        Bypass2Fa = true
        //    };

        //    var pipeline = host.Services.GetRequiredService<RadiusPipeline>();

        //    await pipeline.InvokeAsync(context);

        //    postProcessor.Verify(v => v.InvokeAsync(It.Is<RadiusContext>(x => x == context)), Times.Once);
        //}
    }
}
