using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using MultiFactor.Radius.Adapter.Core.Framework.Context;
using MultiFactor.Radius.Adapter.Core.Framework.Pipeline;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration.RootLevel;
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
    public async Task Invoke_HasNoStateAttribute_ShouldInvokeNext()
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
            chProc.Setup(x => x.HasChallengeContext(It.IsAny<ChallengeIdentifier>())).Returns(false);
            builder.Services.RemoveService<IChallengeProcessor>().AddSingleton(chProc.Object);
        });

        var packet = RadiusPacketFactory.AccessRequest();
        packet.AddAttribute("State", "SomeState");
        var context = host.CreateContext(packet);

        var nextDelegate = new Mock<RadiusRequestDelegate>();

        var middleware = host.Service<AccessChallengeMiddleware>();
        await middleware.InvokeAsync(context, nextDelegate.Object);

        context.State.Should().BeNullOrEmpty();

        nextDelegate.Verify(v => v.Invoke(It.Is<RadiusContext>(x => x == context)), Times.Once);
    }
    
    [Fact]
    public async Task Invoke_HasStateAttributeAndHasChallengeState_ShouldInvokeChallengeProcessor()
    {
        var expectedReqId = "Qwerty123";

        var chProc = new Mock<IChallengeProcessor>();
        chProc.Setup(x => x.HasChallengeContext(It.IsAny<ChallengeIdentifier>())).Returns(true);

        var host = TestHostFactory.CreateHost(builder =>
        {
            builder.UseMiddleware<AccessChallengeMiddleware>();
            builder.Services.Configure<TestConfigProviderOptions>(x =>
            {
                x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-single.config");
            });
            
            var provider = new Mock<IChallengeProcessorProvider>();
            provider.Setup(x => x.GetChallengeProcessorForIdentifier(It.IsAny<ChallengeIdentifier>())).Returns(chProc.Object);
            
            builder.Services.ReplaceService(provider.Object);
        });

        var config = host.Service<IServiceConfiguration>();
        var client = config.Clients[0];
        var packet = RadiusPacketFactory.AccessRequest();
        packet.AddAttribute("State", expectedReqId);
        var context = host.CreateContext(packet, clientConfig: client);
        var expectedIdentifier = new ChallengeIdentifier(client.Name, expectedReqId);

        var pipeline = host.Service<RadiusPipeline>();
        await pipeline.InvokeAsync(context);

        chProc.Verify(v => v.ProcessChallengeAsync(It.Is<ChallengeIdentifier>(x => x.Equals(expectedIdentifier)), It.Is<RadiusContext>(x => x == context)), Times.Once);
    }
    
    [Fact]
    public async Task Invoke_ChallengeIsAccept_ShouldNotTerminatePipeline()
    {
        var expectedReqId = "Qwerty123";

        var host = TestHostFactory.CreateHost(builder =>
        {
            builder.UseMiddleware<AccessChallengeMiddleware>();
            builder.Services.Configure<TestConfigProviderOptions>(x =>
            {
                x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-single.config");
            });

            builder.Services.RemoveAll<IChallengeProcessor>();
            var chProc = new Mock<IChallengeProcessor>();
            var provider = new Mock<IChallengeProcessorProvider>();
            chProc.Setup(x => x.HasChallengeContext(It.IsAny<ChallengeIdentifier>())).Returns(true);
            chProc.Setup(x => x.ProcessChallengeAsync(It.IsAny<ChallengeIdentifier>(), It.IsAny<RadiusContext>())).ReturnsAsync(ChallengeCode.Accept);
            provider.Setup(x => x.GetChallengeProcessorForIdentifier(It.IsAny<ChallengeIdentifier>())).Returns(chProc.Object);

            builder.Services.ReplaceService(provider.Object);
        });

        var config = host.Service<IServiceConfiguration>();
        var client = config.Clients[0];
        var packet = RadiusPacketFactory.AccessRequest();
        packet.AddAttribute("State", expectedReqId);

        var context = host.CreateContext(packet, clientConfig: client);

        var pipeline = host.Service<RadiusPipeline>();
        await pipeline.InvokeAsync(context);
        
        Assert.False(context.Flags.TerminateFlag);
    }
    
    [Theory]
    [InlineData(ChallengeCode.Reject)]
    [InlineData(ChallengeCode.InProcess)]
    public async Task Invoke_ChallengeNotAccepted_ShouldTerminatePipeline(ChallengeCode code)
    {
        var expectedReqId = "Qwerty123";

        var host = TestHostFactory.CreateHost(builder =>
        {
            builder.UseMiddleware<AccessChallengeMiddleware>();
            builder.Services.Configure<TestConfigProviderOptions>(x =>
            {
                x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-single.config");
            });
            var provider = new Mock<IChallengeProcessorProvider>();
            
            var chProc = new Mock<IChallengeProcessor>();
            chProc.Setup(x => x.HasChallengeContext(It.IsAny<ChallengeIdentifier>())).Returns(true);
            chProc.Setup(x => x.ProcessChallengeAsync(It.IsAny<ChallengeIdentifier>(), It.IsAny<RadiusContext>())).ReturnsAsync(code);
            provider.Setup(x => x.GetChallengeProcessorForIdentifier(It.IsAny<ChallengeIdentifier>())).Returns(chProc.Object);
            
            builder.Services.ReplaceService(provider.Object);
        });

        var config = host.Service<IServiceConfiguration>();
        var client = config.Clients[0];
        var packet = RadiusPacketFactory.AccessRequest();
        packet.AddAttribute("State", expectedReqId);
        var context = host.CreateContext(packet, clientConfig: client);

        var pipeline = host.Service<RadiusPipeline>();
        await pipeline.InvokeAsync(context);

        Assert.True(context.Flags.TerminateFlag);
    }
}
