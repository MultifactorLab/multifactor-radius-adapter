using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using MultiFactor.Radius.Adapter.Configuration;
using MultiFactor.Radius.Adapter.Configuration.Core;
using MultiFactor.Radius.Adapter.Core.Radius;
using MultiFactor.Radius.Adapter.Framework.Context;
using MultiFactor.Radius.Adapter.Server;
using MultiFactor.Radius.Adapter.Server.Pipeline.AccessChallenge;
using MultiFactor.Radius.Adapter.Server.Pipeline.SecondFactorAuthentication;
using MultiFactor.Radius.Adapter.Services.MultiFactorApi;
using MultiFactor.Radius.Adapter.Tests.Fixtures;
using MultiFactor.Radius.Adapter.Tests.Fixtures.ConfigLoading;
using MultiFactor.Radius.Adapter.Tests.Fixtures.Radius;
using System.Net;

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
        var context = host.CreateContext(RadiusPacketFactory.AccessRequest(), clientConfig: client, setupContext: x =>
        {
            x.UserName = "UserName";
            x.RemoteEndpoint = new IPEndPoint(IPAddress.Any, 636);
        });
        context.RequestPacket.AddAttribute("User-Name", "#ACSACL#-IP-UserName");
        context.SetFirstFactorAuth(AuthenticationCode.Accept);

        await host.InvokePipeline(context);

        context.ResponseCode.Should().Be(PacketCode.AccessAccept);
        context.Authentication.SecondFactor.Should().Be(AuthenticationCode.Bypass);
    }
    
    [Fact]
    public async Task Invoke_ApiShouldReturnChallengeRequest_ShouldInvokeAddState()
    {
        var chProc = new Mock<ISecondFactorChallengeProcessor>();
        chProc.Setup(x => x.HasState(It.IsAny<ChallengeRequestIdentifier>()))
            .Returns(false);

        var host = TestHostFactory.CreateHost(builder =>
        {
            builder.Services.Configure<TestConfigProviderOptions>(x =>
            {
                x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-single.config");
            });

            var adapter = new Mock<IMultifactorApiAdapter>();
            adapter.Setup(x => x.CreateSecondFactorRequestAsync(It.IsAny<RadiusContext>()))
                .ReturnsAsync(new Services.MultiFactorApi.Models.SecondFactorResponse(PacketCode.AccessChallenge));
            builder.Services.ReplaceService(adapter.Object);

            builder.Services.ReplaceService(chProc.Object);
            builder.UseMiddleware<SecondFactorAuthenticationMiddleware>();
        });

        var config = host.Service<IServiceConfiguration>();
        var context = host.CreateContext(RadiusPacketFactory.AccessRequest(), clientConfig: config.Clients[0], setupContext: x =>
        {
            x.RemoteEndpoint = new IPEndPoint(IPAddress.Any, 636);
            x.UserName = "#ACSACL#-IP-UserName";
            x.State = "Qwerty123";
        });
        var expectedIdentifier = new ChallengeRequestIdentifier(config.Clients[0].Name, "Qwerty123");

        await host.InvokePipeline(context);

        Assert.Equal(PacketCode.AccessChallenge, context.ResponseCode);
        Assert.Equal(AuthenticationCode.Awaiting, context.Authentication.SecondFactor);
        chProc.Verify(v => v.AddState(It.Is<RadiusContext>(x => x == context)), Times.Once);
    }
}
