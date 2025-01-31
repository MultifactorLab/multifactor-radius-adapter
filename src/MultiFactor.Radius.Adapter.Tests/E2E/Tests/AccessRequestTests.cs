using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MultiFactor.Radius.Adapter.Core.Framework;
using MultiFactor.Radius.Adapter.Core.Radius;
using MultiFactor.Radius.Adapter.Extensions;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration.ConfigurationLoading;
using MultiFactor.Radius.Adapter.Server.Pipeline.AccessRequestFilter;
using MultiFactor.Radius.Adapter.Server.Pipeline.FirstFactorAuthentication;
using MultiFactor.Radius.Adapter.Server.Pipeline.StatusServer;
using MultiFactor.Radius.Adapter.Tests.E2E.Constants;
using MultiFactor.Radius.Adapter.Tests.Fixtures;
using MultiFactor.Radius.Adapter.Tests.Fixtures.ConfigLoading;

namespace MultiFactor.Radius.Adapter.Tests.E2E.Tests;

[Collection("Radius e2e")]
public class AccessRequestTests : E2ETestBase
{
    public AccessRequestTests(RadiusFixtures radiusFixtures) : base(radiusFixtures)
    {
    }

    [Fact]
    public async Task SendAuthRequestWithoutCredentials_ShouldReject()
    {
        await StartHostAsync(ConfigureHost);

        var accessRequest = CreateRadiusPacket(
            PacketCode.AccessRequest,
            new Dictionary<string, string>() { { "NAS-Identifier", RadiusAdapterConstants.DefaultNasIdentifier } });
        
        var response = SendPacketAsync(accessRequest);
        
        Assert.NotNull(response);
        Assert.Equal(PacketCode.AccessReject, response.Header.Code);
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
                    { TestEnvironment.GetAssetPath(TestAssetLocation.E2E, RadiusAdapterConfigs.AccessRequestConfig) };
            });
        });

        builder.UseMiddleware<StatusServerMiddleware>();
        builder.UseMiddleware<AccessRequestFilterMiddleware>();
        builder.UseMiddleware<FirstFactorAuthenticationMiddleware>();
    }
}