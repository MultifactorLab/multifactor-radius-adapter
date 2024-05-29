using Microsoft.Extensions.Options;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration.ConfigurationLoading;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration.Models;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration.XmlAppConfiguration;
using System.Reflection;

namespace MultiFactor.Radius.Adapter.Tests.Fixtures.ConfigLoading;

internal class TestRootConfigProvider : IRootConfigurationProvider
{
    private readonly TestConfigProviderOptions _options;

    public TestRootConfigProvider(IOptions<TestConfigProviderOptions> options)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    public RadiusAdapterConfiguration GetRootConfiguration()
    {
        RadiusConfigurationFile rdsRootConfig;

        if (!string.IsNullOrWhiteSpace(_options.RootConfigFilePath))
        {
            rdsRootConfig = new RadiusConfigurationFile(_options.RootConfigFilePath);
        }
        else
        {
            var asm = Assembly.GetAssembly(typeof(RdsEntryPoint));
            if (asm is null)
            {
                throw new Exception("Main assembly not found");
            }

            var path = $"{asm.Location}.config";
            rdsRootConfig = new RadiusConfigurationFile(path);
        }

        var config = RadiusAdapterConfigurationFactory.Create(rdsRootConfig);
        return config;
    }
}
