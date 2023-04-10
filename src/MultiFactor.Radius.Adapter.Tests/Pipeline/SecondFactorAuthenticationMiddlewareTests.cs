using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using MultiFactor.Radius.Adapter.Configuration;
using MultiFactor.Radius.Adapter.Configuration.Core;
using MultiFactor.Radius.Adapter.Core.Pipeline;
using MultiFactor.Radius.Adapter.Core.Radius;
using MultiFactor.Radius.Adapter.Server;
using MultiFactor.Radius.Adapter.Server.Pipeline;
using MultiFactor.Radius.Adapter.Services.MultiFactorApi;
using MultiFactor.Radius.Adapter.Tests.Fixtures;
using MultiFactor.Radius.Adapter.Tests.Fixtures.ConfigLoading;
using MultiFactor.Radius.Adapter.Tests.Fixtures.Radius;
using System.Net;

namespace MultiFactor.Radius.Adapter.Tests.Pipeline;

public class SecondFactorAuthenticationMiddlewareTests
{
    [Fact]
    public async Task Invoke_ShouldInvokeNextDelegateAndPostProcessor()
    {
        var postProcessor = new Mock<IRadiusRequestPostProcessor>();

        var host = TestHostFactory.CreateHost(services =>
        {
            services.RemoveService<IRootConfigurationProvider>().AddSingleton<IRootConfigurationProvider, TestRootConfigProvider>();
            services.RemoveService<IClientConfigurationsProvider>().AddSingleton<IClientConfigurationsProvider, TestClientConfigsProvider>();
            services.Configure<TestConfigProviderOptions>(x =>
            {
                x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-single.config");
            });

            services.RemoveService<IRadiusRequestPostProcessor>().AddSingleton(postProcessor.Object);
        });

        var config = host.Services.GetRequiredService<IServiceConfiguration>();
        var responseSender = new Mock<IRadiusResponseSender>();
        var context = new RadiusContext(config.Clients[0], responseSender.Object, new Mock<IServiceProvider>().Object)
        {
            RemoteEndpoint = new IPEndPoint(IPAddress.Any, 636),
            RequestPacket = RadiusPacketFactory.AccessRequest(),
            Bypass2Fa = false
        };

        var nextDelegate = new Mock<RadiusRequestDelegate>();

        var middleware = host.Services.GetRequiredService<SecondFactorAuthenticationMiddleware>();
        await middleware.InvokeAsync(context, nextDelegate.Object);

        nextDelegate.Verify(q => q.Invoke(It.Is<RadiusContext>(x => x == context)), Times.Once);
        postProcessor.Verify(v => v.InvokeAsync(It.Is<RadiusContext>(x => x == context)), Times.Once);
    }
    
    [Fact]
    public async Task Invoke_UsernameIsEmpty_ShouldSetRejectStateAndNotInvokeApi()
    {
        var api = new Mock<IMultiFactorApiClient>();

        var host = TestHostFactory.CreateHost(services =>
        {
            services.RemoveService<IRootConfigurationProvider>().AddSingleton<IRootConfigurationProvider, TestRootConfigProvider>();
            services.RemoveService<IClientConfigurationsProvider>().AddSingleton<IClientConfigurationsProvider, TestClientConfigsProvider>();
            services.Configure<TestConfigProviderOptions>(x =>
            {
                x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-single.config");
            });

            services.RemoveService<IMultiFactorApiClient>().AddSingleton(api.Object);
        });

        var config = host.Services.GetRequiredService<IServiceConfiguration>();
        var responseSender = new Mock<IRadiusResponseSender>();
        var context = new RadiusContext(config.Clients[0], responseSender.Object, new Mock<IServiceProvider>().Object)
        {
            RemoteEndpoint = new IPEndPoint(IPAddress.Any, 636),
            RequestPacket = RadiusPacketFactory.AccessRequest()
        };

        var middleware = host.Services.GetRequiredService<SecondFactorAuthenticationMiddleware>();
        await middleware.InvokeAsync(context, new Mock<RadiusRequestDelegate>().Object);

        context.ResponseCode.Should().Be(PacketCode.AccessReject);
        api.Verify(v => v.CreateSecondFactorRequest(It.Is<RadiusContext>(x => x == context)), Times.Never);
    }

    [Fact]
    public async Task Invoke_IsVendorAclRequestIsTrue_ShouldSetAcceptStateAndNotInvokeApi()
    {
        var api = new Mock<IMultiFactorApiClient>();

        var host = TestHostFactory.CreateHost(services =>
        {
            services.RemoveService<IRootConfigurationProvider>().AddSingleton<IRootConfigurationProvider, TestRootConfigProvider>();
            services.RemoveService<IClientConfigurationsProvider>().AddSingleton<IClientConfigurationsProvider, TestClientConfigsProvider>();
            services.Configure<TestConfigProviderOptions>(x =>
            {
                x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-single.config");
            });

            services.RemoveService<IMultiFactorApiClient>().AddSingleton(api.Object);
        });

        var client = ClientConfiguration.CreateBuilder("custom", "shared_secret", AuthenticationSource.Radius, "key", "secret")
            .SetActiveDirectoryDomain("domain.local")
            .AddActiveDirectoryGroup("Security Group")
            .Build();

        var responseSender = new Mock<IRadiusResponseSender>();
        var context = new RadiusContext(client, responseSender.Object, new Mock<IServiceProvider>().Object)
        {
            RemoteEndpoint = new IPEndPoint(IPAddress.Any, 636),
            RequestPacket = RadiusPacketFactory.AccessRequest(),
            UserName = "UserName"
        };
        context.RequestPacket.AddAttribute("User-Name", "#ACSACL#-IP-UserName");

        var middleware = host.Services.GetRequiredService<SecondFactorAuthenticationMiddleware>();
        await middleware.InvokeAsync(context, new Mock<RadiusRequestDelegate>().Object);

        context.ResponseCode.Should().Be(PacketCode.AccessAccept);
        api.Verify(v => v.CreateSecondFactorRequest(It.Is<RadiusContext>(x => x == context)), Times.Never);
    }
    
    [Fact]
    public async Task Invoke_ShouldSetAcceptStateAndNotInvokeApi()
    {
        var api = new Mock<IMultiFactorApiClient>();

        var host = TestHostFactory.CreateHost(services =>
        {
            services.RemoveService<IRootConfigurationProvider>().AddSingleton<IRootConfigurationProvider, TestRootConfigProvider>();
            services.RemoveService<IClientConfigurationsProvider>().AddSingleton<IClientConfigurationsProvider, TestClientConfigsProvider>();
            services.Configure<TestConfigProviderOptions>(x =>
            {
                x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-single.config");
            });

            services.RemoveService<IMultiFactorApiClient>().AddSingleton(api.Object);
        });

        var client = ClientConfiguration.CreateBuilder("custom", "shared_secret", AuthenticationSource.Radius, "key", "secret")
            .SetActiveDirectoryDomain("domain.local")
            .AddActiveDirectoryGroup("Security Group")
            .Build();
        var responseSender = new Mock<IRadiusResponseSender>();
        var context = new RadiusContext(client, responseSender.Object, new Mock<IServiceProvider>().Object)
        {
            RemoteEndpoint = new IPEndPoint(IPAddress.Any, 636),
            RequestPacket = RadiusPacketFactory.AccessRequest(),
            UserName = "UserName"
        };
        context.RequestPacket.AddAttribute("User-Name", "#ACSACL#-IP-UserName");

        var middleware = host.Services.GetRequiredService<SecondFactorAuthenticationMiddleware>();
        await middleware.InvokeAsync(context, new Mock<RadiusRequestDelegate>().Object);

        context.ResponseCode.Should().Be(PacketCode.AccessAccept);
        api.Verify(v => v.CreateSecondFactorRequest(It.Is<RadiusContext>(x => x == context)), Times.Never);
    }
    
    [Fact]
    public async Task Invoke_ApiShouldReturnChallengeRequest_ShouldInvokeAddState()
    {
        var chProc = new Mock<IChallengeProcessor>();
        chProc.Setup(x => x.HasState(It.IsAny<ChallengeRequestIdentifier>())).Returns(false);

        var host = TestHostFactory.CreateHost(services =>
        {
            services.RemoveService<IRootConfigurationProvider>().AddSingleton<IRootConfigurationProvider, TestRootConfigProvider>();
            services.RemoveService<IClientConfigurationsProvider>().AddSingleton<IClientConfigurationsProvider, TestClientConfigsProvider>();
            services.Configure<TestConfigProviderOptions>(x =>
            {
                x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-single.config");
            });

            var api = new Mock<IMultiFactorApiClient>();
            api.Setup(x => x.CreateSecondFactorRequest(It.IsAny<RadiusContext>())).ReturnsAsync(PacketCode.AccessChallenge);

            services.RemoveService<IMultiFactorApiClient>().AddSingleton(api.Object);  
            services.RemoveService<IChallengeProcessor>().AddSingleton(chProc.Object);
        });

        var config = host.Services.GetRequiredService<IServiceConfiguration>();
        var responseSender = new Mock<IRadiusResponseSender>();
        var context = new RadiusContext(config.Clients[0], responseSender.Object, new Mock<IServiceProvider>().Object)
        {
            RemoteEndpoint = new IPEndPoint(IPAddress.Any, 636),
            RequestPacket = RadiusPacketFactory.AccessRequest(),
            UserName = "#ACSACL#-IP-UserName",
            State = "Qwerty123"
        };
        var expectedIdentifier = new ChallengeRequestIdentifier(config.Clients[0], "Qwerty123");

        var middleware = host.Services.GetRequiredService<SecondFactorAuthenticationMiddleware>();
        await middleware.InvokeAsync(context, new Mock<RadiusRequestDelegate>().Object);

        context.ResponseCode.Should().Be(PacketCode.AccessChallenge);
        chProc.Verify(v => v.AddState(It.Is<ChallengeRequestIdentifier>(x => x.Equals(expectedIdentifier)), It.Is<RadiusContext>(x => x == context)), Times.Once);
    }
}
