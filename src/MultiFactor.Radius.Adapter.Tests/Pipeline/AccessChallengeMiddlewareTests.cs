using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using MultiFactor.Radius.Adapter.Configuration.Core;
using MultiFactor.Radius.Adapter.Framework.Context;
using MultiFactor.Radius.Adapter.Framework.Pipeline;
using MultiFactor.Radius.Adapter.Server;
using MultiFactor.Radius.Adapter.Server.Pipeline.AccessChallenge;
using MultiFactor.Radius.Adapter.Tests.Fixtures;
using MultiFactor.Radius.Adapter.Tests.Fixtures.ConfigLoading;
using MultiFactor.Radius.Adapter.Tests.Fixtures.Radius;

namespace MultiFactor.Radius.Adapter.Tests.Pipeline;

[Trait("Category", "Pipeline")]
public class AccessChallengeMiddlewareTests
{
    [Fact]
    public async Task Invoke_HasNotStateAttribute_ShouldInvokeNext()
    {
        var host = TestHostFactory.CreateHost(builder =>
        {
            builder.UseMiddleware<AccessChallengeMiddleware>();
            builder.Services.Configure<TestConfigProviderOptions>(x =>
            {
                x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-single.config");
            });
        });

        var config = host.Service<IServiceConfiguration>();
        var responseSender = new Mock<IRadiusResponseSender>();
        var context = host.CreateContext(RadiusPacketFactory.AccessRequest());

        var nextDelegate = new Mock<RadiusRequestDelegate>();

        var middleware = host.Service<AccessChallengeMiddleware>();
        await middleware.InvokeAsync(context, nextDelegate.Object);

        nextDelegate.Verify(q => q.Invoke(It.Is<RadiusContext>(x => x == context)), Times.Once);
    }
    
    [Fact]
    public async Task Invoke_HasStateAttributeAndHasNotChallengeState_ShouldInvokeNext()
    {
        var host = TestHostFactory.CreateHost(builder =>
        {
            builder.UseMiddleware<AccessChallengeMiddleware>();
            builder.Services.Configure<TestConfigProviderOptions>(x =>
            {
                x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-single.config");
            });

            var chProc = new Mock<IChallengeProcessor>();
            chProc.Setup(x => x.HasState(It.IsAny<ChallengeRequestIdentifier>())).Returns(false);
            builder.Services.RemoveService<IChallengeProcessor>().AddSingleton(chProc.Object);
        });

        var packet = RadiusPacketFactory.AccessRequest(x =>
        {
            x.AddAttribute("State", "SomeState");
        });
        var context = host.CreateContext(packet);

        var nextDelegate = new Mock<RadiusRequestDelegate>();

        var middleware = host.Service<AccessChallengeMiddleware>();
        await middleware.InvokeAsync(context, nextDelegate.Object);

        context.State.Should().BeNullOrEmpty();

        nextDelegate.Verify(v => v.Invoke(It.Is<RadiusContext>(x => x == context)), Times.Once);
    }
    
    [Fact]
    public async Task Invoke_HasStateAttributeAndHasChallengeState_ShouldInvokePostProcessorAndChallengeProcessor()
    {
        var expectedReqId = "Qwerty123";
        var postProcessor = new Mock<IRadiusRequestPostProcessor>();

        var chProc = new Mock<IChallengeProcessor>();
        chProc.Setup(x => x.HasState(It.IsAny<ChallengeRequestIdentifier>())).Returns(true);

        var host = TestHostFactory.CreateHost(builder =>
        {
            builder.UseMiddleware<AccessChallengeMiddleware>();
            builder.Services.Configure<TestConfigProviderOptions>(x =>
            {
                x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-single.config");
            });

            builder.Services.ReplaceService(postProcessor.Object);
            builder.Services.ReplaceService(chProc.Object);
        });

        var config = host.Service<IServiceConfiguration>();
        var client = config.Clients[0];
        var packet = RadiusPacketFactory.AccessRequest(x =>
        {
            x.AddAttribute("State", expectedReqId);
        });
        var context = host.CreateContext(packet, clientConfig: client);
        var expectedIdentifier = new ChallengeRequestIdentifier(client, expectedReqId);

        var pipeline = host.Service<RadiusPipeline>();
        await pipeline.InvokeAsync(context);

        context.State.Should().Be(expectedReqId);

        postProcessor.Verify(v => v.InvokeAsync(It.Is<RadiusContext>(x => x == context)), Times.Once);
        chProc.Verify(v => v.ProcessChallengeAsync(It.Is<ChallengeRequestIdentifier>(x => x.Equals(expectedIdentifier)), It.Is<RadiusContext>(x => x == context)), Times.Once);
    }
}
