using System.Reflection;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configuration.RadiusAdapter;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configuration.RadiusAdapter.Build;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configuration.XmlAppConfiguration;
using Multifactor.Radius.Adapter.v2.Server;

namespace Multifactor.Radius.Adapter.v2.EndToEndTests.Fixtures.ConfigLoading;

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
            var asm = Assembly.GetAssembly(typeof(AdapterServer));
            if (asm is null)
                throw new Exception("Main assembly not found");

            var path = $"{asm.Location}.config";
            rdsRootConfig = new RadiusConfigurationFile(path);
        }

        var config = RadiusAdapterConfigurationFactory.Create(rdsRootConfig);
        return config;
    }
}
