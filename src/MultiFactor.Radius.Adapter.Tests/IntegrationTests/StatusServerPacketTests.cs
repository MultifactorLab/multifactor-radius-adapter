using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration.ConfigurationLoading;
using MultiFactor.Radius.Adapter.Tests.Fixtures;
using MultiFactor.Radius.Adapter.Tests.Fixtures.ConfigLoading;

namespace MultiFactor.Radius.Adapter.Tests.IntegrationTests;

public class StatusServerPacketTests : IClassFixture<WebApplicationFactory<RdsEntryPoint>>
{
    private readonly WebApplicationFactory<RdsEntryPoint> _factory;

    public StatusServerPacketTests(WebApplicationFactory<RdsEntryPoint> factory)
    {
        _factory = factory;
    }
    
    [Fact]
    public async Task ShouldReturnResponse()
    {
        var factory = _factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
        {
            services.AddOptions<TestConfigProviderOptions>();
            services.Configure<TestConfigProviderOptions>(x =>
            {
                x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-single.config");
            });
            services.ReplaceService(prov =>
            {
                var opt = prov.GetRequiredService<IOptions<TestConfigProviderOptions>>().Value;
                var rootConfig = TestRootConfigProvider.GetRootConfiguration(opt);
                var factory = prov.GetRequiredService<ServiceConfigurationFactory>();

                var config = factory.CreateConfig(rootConfig);
                config.Validate();

                return config;
            });
            
            services.ReplaceService<ILoggerFactory, NullLoggerFactory>();
        }));
    }
}