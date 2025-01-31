using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MultiFactor.Radius.Adapter.Core.Framework;
using MultiFactor.Radius.Adapter.Core.Radius;
using MultiFactor.Radius.Adapter.Extensions;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration.ConfigurationLoading;
using MultiFactor.Radius.Adapter.Server.Pipeline.StatusServer;
using MultiFactor.Radius.Adapter.Tests.E2E.Constants;
using MultiFactor.Radius.Adapter.Tests.Fixtures;
using MultiFactor.Radius.Adapter.Tests.Fixtures.ConfigLoading;

namespace MultiFactor.Radius.Adapter.Tests.E2E.Tests;

[Collection("Radius e2e")]
public class StatusServerTests : E2ETestBase
{
    public StatusServerTests(RadiusFixtures radiusFixtures) : base(radiusFixtures)
    {
    }

    [Fact]
    public async Task GetServerStatus_ShouldSuccess()
    {
        await StartHostAsync(ConfigureHost);

        var serverStatusPacket = CreateRadiusPacket(
            PacketCode.StatusServer,
            new Dictionary<string, string>() { { "NAS-Identifier", RadiusAdapterConstants.DefaultNasIdentifier } });

        var response = SendPacketAsync(serverStatusPacket);
        
        Assert.NotNull(response);
        Assert.Equal(PacketCode.AccessAccept, response.Header.Code);
        
        var replyMessage = response.Attributes["Reply-Message"].FirstOrDefault()?.ToString();
        Assert.NotNull(replyMessage);
        Assert.StartsWith("Server up", replyMessage);
    }

    private void ConfigureHost(RadiusHostApplicationBuilder builder)
    {
        builder.AddLogging();

        builder.Services.AddOptions<TestConfigProviderOptions>();

        builder.Services.ReplaceService(prov =>
        {
            var opt = prov.GetRequiredService<IOptions<TestConfigProviderOptions>>().Value;
            var rootConfig = TestRootConfigProvider.GetRootConfiguration(opt);
            var factory = prov.GetRequiredService<ServiceConfigurationFactory>();

            var config = factory.CreateConfig(rootConfig);
            config.Validate();

            return config;
        });

        builder.Services.ReplaceService<IClientConfigurationsProvider, TestClientConfigsProvider>();

        builder.ConfigureApplication(b =>
        {
            b.Configure<TestConfigProviderOptions>(x =>
            {
                x.RootConfigFilePath = TestEnvironment.GetAssetPath(TestAssetLocation.E2E, RadiusAdapterConfigs.RootConfig);
                x.ClientConfigFilePaths = new[]
                    { TestEnvironment.GetAssetPath(TestAssetLocation.E2E, RadiusAdapterConfigs.StatusServerConfig) };
            });
        });

        builder.UseMiddleware<StatusServerMiddleware>();
    }
}