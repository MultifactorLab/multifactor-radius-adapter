using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using MultiFactor.Radius.Adapter.Core.Framework.Context;
using MultiFactor.Radius.Adapter.Core.Framework.Pipeline;
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
