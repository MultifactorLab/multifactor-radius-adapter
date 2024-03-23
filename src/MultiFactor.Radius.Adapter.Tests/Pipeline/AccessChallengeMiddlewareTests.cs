﻿using FluentAssertions;
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

            var chProc = new Mock<ISecondFactorChallengeProcessor>();
            chProc.Setup(x => x.HasState(It.IsAny<ChallengeRequestIdentifier>())).Returns(false);
            builder.Services.RemoveService<ISecondFactorChallengeProcessor>().AddSingleton(chProc.Object);
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
    public async Task Invoke_HasStateAttributeAndHasChallengeState_ShouldInvokeChallengeProcessor()
    {
        var expectedReqId = "Qwerty123";

        var chProc = new Mock<ISecondFactorChallengeProcessor>();
        chProc.Setup(x => x.HasState(It.IsAny<ChallengeRequestIdentifier>())).Returns(true);

        var host = TestHostFactory.CreateHost(builder =>
        {
            builder.UseMiddleware<AccessChallengeMiddleware>();
            builder.Services.Configure<TestConfigProviderOptions>(x =>
            {
                x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-single.config");
            });

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

        chProc.Verify(v => v.ProcessChallengeAsync(It.Is<ChallengeRequestIdentifier>(x => x.Equals(expectedIdentifier)), It.Is<RadiusContext>(x => x == context)), Times.Once);
    }
    
    [Fact]
    public async Task Invoke_ChallengeIsAccept_SecondFactorShouldAccept()
    {
        var expectedReqId = "Qwerty123";

        var host = TestHostFactory.CreateHost(builder =>
        {
            builder.UseMiddleware<AccessChallengeMiddleware>();
            builder.Services.Configure<TestConfigProviderOptions>(x =>
            {
                x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-single.config");
            });

            var chProc = new Mock<ISecondFactorChallengeProcessor>();
            chProc.Setup(x => x.HasState(It.IsAny<ChallengeRequestIdentifier>())).Returns(true);
            chProc.Setup(x => x.ProcessChallengeAsync(It.IsAny<ChallengeRequestIdentifier>(), It.IsAny<RadiusContext>())).ReturnsAsync(ChallengeCode.Accept);
            builder.Services.ReplaceService(chProc.Object);
        });

        var config = host.Service<IServiceConfiguration>();
        var client = config.Clients[0];
        var packet = RadiusPacketFactory.AccessRequest(x =>
        {
            x.AddAttribute("State", expectedReqId);
        });
        var context = host.CreateContext(packet, clientConfig: client);

        var pipeline = host.Service<RadiusPipeline>();
        await pipeline.InvokeAsync(context);

        Assert.Equal(AuthenticationCode.Accept, context.Authentication.SecondFactor);
    }
    
    [Fact]
    public async Task Invoke_ChallengeIsReject_SecondFactorShouldReject()
    {
        var expectedReqId = "Qwerty123";

        var host = TestHostFactory.CreateHost(builder =>
        {
            builder.UseMiddleware<AccessChallengeMiddleware>();
            builder.Services.Configure<TestConfigProviderOptions>(x =>
            {
                x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-single.config");
            });

            var chProc = new Mock<ISecondFactorChallengeProcessor>();
            chProc.Setup(x => x.HasState(It.IsAny<ChallengeRequestIdentifier>())).Returns(true);
            chProc.Setup(x => x.ProcessChallengeAsync(It.IsAny<ChallengeRequestIdentifier>(), It.IsAny<RadiusContext>())).ReturnsAsync(ChallengeCode.Reject);
            builder.Services.ReplaceService(chProc.Object);
        });

        var config = host.Service<IServiceConfiguration>();
        var client = config.Clients[0];
        var packet = RadiusPacketFactory.AccessRequest(x =>
        {
            x.AddAttribute("State", expectedReqId);
        });
        var context = host.CreateContext(packet, clientConfig: client);

        var pipeline = host.Service<RadiusPipeline>();
        await pipeline.InvokeAsync(context);

        Assert.Equal(AuthenticationCode.Reject, context.Authentication.SecondFactor);
    }
    
    [Fact]
    public async Task Invoke_ChallengeIsInProcess_SecondFactorShouldAwaiting()
    {
        var expectedReqId = "Qwerty123";

        var host = TestHostFactory.CreateHost(builder =>
        {
            builder.UseMiddleware<AccessChallengeMiddleware>();
            builder.Services.Configure<TestConfigProviderOptions>(x =>
            {
                x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-single.config");
            });

            var chProc = new Mock<ISecondFactorChallengeProcessor>();
            chProc.Setup(x => x.HasState(It.IsAny<ChallengeRequestIdentifier>())).Returns(true);
            chProc.Setup(x => x.ProcessChallengeAsync(It.IsAny<ChallengeRequestIdentifier>(), It.IsAny<RadiusContext>())).ReturnsAsync(ChallengeCode.InProcess);
            builder.Services.ReplaceService(chProc.Object);
        });

        var config = host.Service<IServiceConfiguration>();
        var client = config.Clients[0];
        var packet = RadiusPacketFactory.AccessRequest(x =>
        {
            x.AddAttribute("State", expectedReqId);
        });
        var context = host.CreateContext(packet, clientConfig: client);

        var pipeline = host.Service<RadiusPipeline>();
        await pipeline.InvokeAsync(context);

        Assert.Equal(AuthenticationCode.Awaiting, context.Authentication.SecondFactor);
    }
}
