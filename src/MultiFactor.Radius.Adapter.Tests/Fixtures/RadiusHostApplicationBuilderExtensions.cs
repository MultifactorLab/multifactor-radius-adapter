using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using MultiFactor.Radius.Adapter.Core.Framework;
using MultiFactor.Radius.Adapter.Core.Framework.Pipeline;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration.ConfigurationLoading;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration.Core;
using MultiFactor.Radius.Adapter.Tests.Fixtures.ConfigLoading;

namespace MultiFactor.Radius.Adapter.Tests.Fixtures;

internal static class RadiusHostApplicationBuilderExtensions
{
    public static RadiusHostApplicationBuilder WithTestConfiguration(this RadiusHostApplicationBuilder builder, 
        Action<TestConfigProviderOptions>? configure = null)
    {
        builder.Services.AddOptions<TestConfigProviderOptions>();
        if (configure is not null)
        {
            builder.Services.Configure(configure);
        }
        
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
        
        return builder;
    }

    public static RadiusHostApplicationBuilder MockUdp(this RadiusHostApplicationBuilder builder)
    {
        builder.Services.ReplaceService<Func<IPEndPoint, IUdpClient>>(prov => endpoint => new Mock<IUdpClient>().Object);
        return builder;
    }
    
    public static RadiusHostApplicationBuilder MockRadiusRequestPostProcessor(this RadiusHostApplicationBuilder builder)
    {
        builder.Services.ReplaceService(new Mock<IRadiusRequestPostProcessor>().Object);
        return builder;
    }
}