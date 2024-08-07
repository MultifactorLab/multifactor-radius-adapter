﻿using Moq;
using MultiFactor.Radius.Adapter.Tests.Fixtures.Radius;
using MultiFactor.Radius.Adapter.Tests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using MultiFactor.Radius.Adapter.Tests.Fixtures.ConfigLoading;
using MultiFactor.Radius.Adapter.Core.Framework.Context;
using MultiFactor.Radius.Adapter.Core.Framework.Pipeline;

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
            });

            var context = host.CreateContext(RadiusPacketFactory.AccessRequest());
            var pipeline = host.Service<RadiusPipeline>();

            await pipeline.InvokeAsync(context);

            postProcessor.Verify(v => v.InvokeAsync(It.Is<RadiusContext>(x => x == context)), Times.Once);
        }
    }
}
