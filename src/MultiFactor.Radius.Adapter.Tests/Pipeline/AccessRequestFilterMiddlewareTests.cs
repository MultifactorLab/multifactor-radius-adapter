using Microsoft.Extensions.DependencyInjection;
using Moq;
using MultiFactor.Radius.Adapter.Framework.Context;
using MultiFactor.Radius.Adapter.Framework.Pipeline;
using MultiFactor.Radius.Adapter.Server.Pipeline.AccessRequestFilter;
using MultiFactor.Radius.Adapter.Tests.Fixtures;
using MultiFactor.Radius.Adapter.Tests.Fixtures.ConfigLoading;
using MultiFactor.Radius.Adapter.Tests.Fixtures.Radius;

namespace MultiFactor.Radius.Adapter.Tests.Pipeline
{
    [Trait("Category", "Pipeline")]
    public class AccessRequestFilterMiddlewareTests
    {
        [Fact]
        public async Task Invoke_AccessRequest_ShouldInvokeNext()
        {
            var host = TestHostFactory.CreateHost(builder =>
            {
                builder.Services.Configure<TestConfigProviderOptions>(x =>
                {
                    x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-single.config");
                });
                builder.UseMiddleware<AccessRequestFilterMiddleware>();
            });

            var context = host.CreateContext(RadiusPacketFactory.AccessRequest());
            var nextDelegate = new Mock<RadiusRequestDelegate>();

            var middleware = host.Service<AccessRequestFilterMiddleware>();
            await middleware.InvokeAsync(context, nextDelegate.Object);

            nextDelegate.Verify(q => q.Invoke(It.Is<RadiusContext>(x => x == context)), Times.Once);
        }
        
        [Fact]
        public async Task Invoke_NonAccessRequest_ShouldNotInvokeNext()
        {
            var host = TestHostFactory.CreateHost(builder =>
            {
                builder.Services.Configure<TestConfigProviderOptions>(x =>
                {
                    x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-single.config");
                });
                builder.UseMiddleware<AccessRequestFilterMiddleware>();
            });

            var context = host.CreateContext(RadiusPacketFactory.StatusServer());
            var nextDelegate = new Mock<RadiusRequestDelegate>();

            var middleware = host.Service<AccessRequestFilterMiddleware>();
            await middleware.InvokeAsync(context, nextDelegate.Object);

            nextDelegate.Verify(q => q.Invoke(It.Is<RadiusContext>(x => x == context)), Times.Never);
        }
    }
}
