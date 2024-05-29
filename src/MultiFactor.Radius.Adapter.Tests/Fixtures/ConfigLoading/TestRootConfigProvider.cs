using MultiFactor.Radius.Adapter.Infrastructure.Configuration.ConfigurationLoading;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration.Models;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration.XmlAppConfiguration;
using System.Reflection;

namespace MultiFactor.Radius.Adapter.Tests.Fixtures.ConfigLoading;

internal static class TestRootConfigProvider
{
    public static RadiusAdapterConfiguration GetRootConfiguration(TestConfigProviderOptions options)
    {
        RadiusConfigurationFile rdsRootConfig;

        if (!string.IsNullOrWhiteSpace(options.RootConfigFilePath))
        {
            rdsRootConfig = new RadiusConfigurationFile(options.RootConfigFilePath);
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
