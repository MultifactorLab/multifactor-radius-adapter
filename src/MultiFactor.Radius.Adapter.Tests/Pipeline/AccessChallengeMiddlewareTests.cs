using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using MultiFactor.Radius.Adapter.Configuration.Core;
using MultiFactor.Radius.Adapter.Core.Pipeline;
using MultiFactor.Radius.Adapter.Server;
using MultiFactor.Radius.Adapter.Server.Context;
using MultiFactor.Radius.Adapter.Server.Pipeline;
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

        var middleware = host.Services.GetRequiredService<AccessChallengeMiddleware>();
        await middleware.InvokeAsync(context, nextDelegate.Object);

        nextDelegate.Verify(q => q.Invoke(It.Is<RadiusContext>(x => x == context)), Times.Once);
    }
    
    [Fact]
    public async Task Invoke_HasStateAttributeAndHasNotChallengeState_ShouldInvokeNext()
    {
        var host = TestHostFactory.CreateHost(services =>
        {
            services.RemoveService<IRootConfigurationProvider>().AddSingleton<IRootConfigurationProvider, TestRootConfigProvider>();
            services.RemoveService<IClientConfigurationsProvider>().AddSingleton<IClientConfigurationsProvider, TestClientConfigsProvider>();
            services.Configure<TestConfigProviderOptions>(x =>
            {
                x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-single.config");
            });

            var chProc = new Mock<IChallengeProcessor>();
            chProc.Setup(x => x.HasState(It.IsAny<ChallengeRequestIdentifier>())).Returns(false);
            services.RemoveService<IChallengeProcessor>().AddSingleton(chProc.Object);
        });

        var config = host.Services.GetRequiredService<IServiceConfiguration>();
        var responseSender = new Mock<IRadiusResponseSender>();
        var context = new RadiusContext(config.Clients[0], responseSender.Object, new Mock<IServiceProvider>().Object)
        {
            RequestPacket = RadiusPacketFactory.AccessRequest()
        };
        context.RequestPacket.AddAttribute("State", "SomeState");

        var nextDelegate = new Mock<RadiusRequestDelegate>();

        var middleware = host.Services.GetRequiredService<AccessChallengeMiddleware>();
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

        var host = TestHostFactory.CreateHost(services =>
        {
            services.RemoveService<IRootConfigurationProvider>().AddSingleton<IRootConfigurationProvider, TestRootConfigProvider>();
            services.RemoveService<IClientConfigurationsProvider>().AddSingleton<IClientConfigurationsProvider, TestClientConfigsProvider>();
            services.Configure<TestConfigProviderOptions>(x =>
            {
                x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-single.config");
            });

            services.RemoveService<IRadiusRequestPostProcessor>().AddSingleton(postProcessor.Object);
            services.RemoveService<IChallengeProcessor>().AddSingleton(chProc.Object);
        });

        var config = host.Services.GetRequiredService<IServiceConfiguration>();
        var client = config.Clients[0];
        var responseSender = new Mock<IRadiusResponseSender>();
        var context = new RadiusContext(client, responseSender.Object, new Mock<IServiceProvider>().Object)
        {
            RequestPacket = RadiusPacketFactory.AccessRequest()
        };
        context.RequestPacket.AddAttribute("State", expectedReqId);

        var expectedIdentifier = new ChallengeRequestIdentifier(client, expectedReqId);

        var nextDelegate = new Mock<RadiusRequestDelegate>();

        var middleware = host.Services.GetRequiredService<AccessChallengeMiddleware>();
        await middleware.InvokeAsync(context, nextDelegate.Object);

        context.State.Should().Be(expectedReqId);

        nextDelegate.Verify(v => v.Invoke(It.IsAny<RadiusContext>()), Times.Never);
        postProcessor.Verify(v => v.InvokeAsync(It.Is<RadiusContext>(x => x == context)), Times.Once);
        chProc.Verify(v => v.ProcessChallengeAsync(It.Is<ChallengeRequestIdentifier>(x => x.Equals(expectedIdentifier)), It.Is<RadiusContext>(x => x == context)), Times.Once);
    }
}
