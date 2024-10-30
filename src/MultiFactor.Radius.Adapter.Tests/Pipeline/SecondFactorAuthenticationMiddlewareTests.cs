using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using MultiFactor.Radius.Adapter.Core.Framework.Context;
using MultiFactor.Radius.Adapter.Core.Radius;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration;
using MultiFactor.Radius.Adapter.Infrastructure.Http;
using MultiFactor.Radius.Adapter.Server;
using MultiFactor.Radius.Adapter.Server.Pipeline.AccessChallenge;
using MultiFactor.Radius.Adapter.Server.Pipeline.SecondFactorAuthentication;
using MultiFactor.Radius.Adapter.Services.MultiFactorApi;
using MultiFactor.Radius.Adapter.Services.MultiFactorApi.Dto;
using MultiFactor.Radius.Adapter.Tests.Fixtures;
using MultiFactor.Radius.Adapter.Tests.Fixtures.ConfigLoading;
using MultiFactor.Radius.Adapter.Tests.Fixtures.Radius;
using System.Net;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration.ClientLevel;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration.RootLevel;

namespace MultiFactor.Radius.Adapter.Tests.Pipeline;

[Trait("Category", "Pipeline")]
public class SecondFactorAuthenticationMiddlewareTests
{     
    [Fact]
    public async Task Invoke_UsernameIsEmpty_ShouldSetReject()
    {
        var api = new Mock<IMultifactorApiClient>();

        var host = TestHostFactory.CreateHost(builder =>
        {
            builder.Services.Configure<TestConfigProviderOptions>(x =>
            {
                x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-single.config");
            });

            builder.Services.RemoveService<IMultifactorApiClient>().AddSingleton(api.Object);
            builder.UseMiddleware<SecondFactorAuthenticationMiddleware>();
        });

        var context = host.CreateContext(RadiusPacketFactory.AccessRequest(), setupContext: x =>
        {
            x.RemoteEndpoint = new IPEndPoint(IPAddress.Any, 636);
        });

        await host.InvokePipeline(context);

        context.ResponseCode.Should().Be(PacketCode.AccessReject);
    }

    [Fact]
    public async Task Invoke_IsVendorAclRequestIsTrue_ShouldSetAcceptState()
    {
        var host = TestHostFactory.CreateHost(builder =>
        {
            builder.Services.Configure<TestConfigProviderOptions>(x =>
            {
                x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-single.config");
            });

            builder.Services.ReplaceService(new Mock<IMultifactorApiClient>().Object);
            builder.UseMiddleware<SecondFactorAuthenticationMiddleware>();
        });

        var client = new ClientConfiguration("custom", "shared_secret", AuthenticationSource.Radius, "key", "secret")
            .SetActiveDirectoryDomain("domain.local")
            .AddActiveDirectoryGroup("Security Group");
        var packet = RadiusPacketFactory.AccessRequest();
        var context = host.CreateContext(packet, clientConfig: client, setupContext: x =>
        {
            x.RemoteEndpoint = new IPEndPoint(IPAddress.Any, 636);
        });
        context.RequestPacket.AddAttribute("User-Name", "#ACSACL#-IP-UserName");
        context.SetFirstFactorAuth(AuthenticationCode.Accept);

        await host.InvokePipeline(context);

        context.ResponseCode.Should().Be(PacketCode.AccessAccept);
        context.Authentication.SecondFactor.Should().Be(AuthenticationCode.Bypass);
    }

    [Fact]
    public async Task Invoke_ApiUnreachErrorAndBypassSettingIsEnabled_ShouldBypass()
    {
        var host = TestHostFactory.CreateHost(builder =>
        {
            builder.Services.Configure<TestConfigProviderOptions>(x =>
            {
                x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-single.config");
            });

            var api = new Mock<IMultifactorApiClient>();
            api.Setup(x => x.CreateRequestAsync(It.IsAny<CreateRequestDto>(), It.IsAny<BasicAuthHeaderValue>()))
                .ThrowsAsync(new MultifactorApiUnreachableException());

            builder.Services.ReplaceService(api.Object);
            builder.UseMiddleware<SecondFactorAuthenticationMiddleware>();
        });

        var client = new ClientConfiguration("custom", "shared_secret", AuthenticationSource.None, "key", "secret")
            .SetBypassSecondFactorWhenApiUnreachable(true);

        var packet = RadiusPacketFactory.AccessRequest();
        packet.AddAttribute("User-Name", "UserName");
        var context = host.CreateContext(packet, clientConfig: client, setupContext: x =>
        {
            x.RemoteEndpoint = new IPEndPoint(IPAddress.Any, 636);
        });

        // first factor midleware was invoked
        context.SetFirstFactorAuth(AuthenticationCode.Accept);

        await host.InvokePipeline(context);

        context.ResponseCode.Should().Be(PacketCode.AccessAccept);
        context.Authentication.SecondFactor.Should().Be(AuthenticationCode.Bypass);
    }
    
    [Fact]
    public async Task Invoke_ApiUnreachErrorAndBypassSettingIsDisabled_ShouldReject()
    {
        var host = TestHostFactory.CreateHost(builder =>
        {
            builder.Services.Configure<TestConfigProviderOptions>(x =>
            {
                x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-single.config");
            });

            var api = new Mock<IMultifactorApiClient>();
            api.Setup(x => x.CreateRequestAsync(It.IsAny<CreateRequestDto>(), It.IsAny<BasicAuthHeaderValue>()))
                .ThrowsAsync(new MultifactorApiUnreachableException());

            builder.Services.ReplaceService(api.Object);
            builder.UseMiddleware<SecondFactorAuthenticationMiddleware>();
        });

        var client = new ClientConfiguration("custom", "shared_secret", AuthenticationSource.None, "key", "secret")
            .SetBypassSecondFactorWhenApiUnreachable(false);

        var packet = RadiusPacketFactory.AccessRequest();
        packet.AddAttribute("User-Name", "UserName");
        var context = host.CreateContext(RadiusPacketFactory.AccessRequest(), clientConfig: client, setupContext: x =>
        {
            x.RemoteEndpoint = new IPEndPoint(IPAddress.Any, 636);
        });

        // first factor midleware was invoked
        context.SetFirstFactorAuth(AuthenticationCode.Accept);

        await host.InvokePipeline(context);

        context.ResponseCode.Should().Be(PacketCode.AccessReject);
        context.Authentication.SecondFactor.Should().Be(AuthenticationCode.Reject);
    }

    [Fact]
    public async Task Invoke_ApiCommonErrorAndBypassSettingIsEnabled_ShouldReject()
    {
        var host = TestHostFactory.CreateHost(builder =>
        {
            builder.Services.Configure<TestConfigProviderOptions>(x =>
            {
                x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-single.config");
            });

            var api = new Mock<IMultifactorApiClient>();
            api.Setup(x => x.CreateRequestAsync(It.IsAny<CreateRequestDto>(), It.IsAny<BasicAuthHeaderValue>()))
                .ThrowsAsync(new Exception());

            builder.Services.ReplaceService(api.Object);
            builder.UseMiddleware<SecondFactorAuthenticationMiddleware>();
        });

        var client = new ClientConfiguration("custom", "shared_secret", AuthenticationSource.None, "key", "secret")
            .SetBypassSecondFactorWhenApiUnreachable(true);

        var packet = RadiusPacketFactory.AccessRequest();
        packet.AddAttribute("User-Name", "UserName");
        var context = host.CreateContext(packet, clientConfig: client, setupContext: x =>
        {
            x.RemoteEndpoint = new IPEndPoint(IPAddress.Any, 636);
        });

        // first factor midleware was invoked
        context.SetFirstFactorAuth(AuthenticationCode.Accept);

        await host.InvokePipeline(context);

        context.ResponseCode.Should().Be(PacketCode.AccessReject);
        context.Authentication.SecondFactor.Should().Be(AuthenticationCode.Reject);
    }

    [Fact]
    public async Task Invoke_ApiShouldReturnChallengeRequest_ShouldInvokeAddState()
    {
        var chProc = new Mock<IChallengeProcessor>();
        chProc.Setup(x => x.HasChallengeContext(It.IsAny<ChallengeIdentifier>()))
            .Returns(false);

        var host = TestHostFactory.CreateHost(builder =>
        {
            builder.Services.Configure<TestConfigProviderOptions>(x =>
            {
                x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-single.config");
            });

            var adapter = new Mock<IMultifactorApiAdapter>();
            adapter.Setup(x => x.CreateSecondFactorRequestAsync(It.IsAny<RadiusContext>()))
                .ReturnsAsync(new Services.MultiFactorApi.Models.SecondFactorResponse(AuthenticationCode.Awaiting));
            builder.Services.ReplaceService(adapter.Object);
            var provider = new Mock<IChallengeProcessorProvider>();
            provider.Setup(x => x.GetChallengeProcessorByType(It.IsAny<ChallengeType>())).Returns(chProc.Object);
            
            builder.Services.ReplaceService(provider.Object);
            
            builder.UseMiddleware<SecondFactorAuthenticationMiddleware>();
        });

        var config = host.Service<IServiceConfiguration>();
        var packet = RadiusPacketFactory.AccessRequest();
        packet.AddAttribute("User-Name", "#ACSACL#-IP-UserName");
        var context = host.CreateContext(packet, clientConfig: config.Clients[0], setupContext: x =>
        {
            x.RemoteEndpoint = new IPEndPoint(IPAddress.Any, 636);
        });
        context.SetMessageState("Qwerty123");

        await host.InvokePipeline(context);

        Assert.Equal(PacketCode.AccessChallenge, context.ResponseCode);
        Assert.Equal(AuthenticationCode.Awaiting, context.Authentication.SecondFactor);
        chProc.Verify(v => v.AddChallengeContext(It.Is<RadiusContext>(x => x == context)), Times.Once);
    }
}
