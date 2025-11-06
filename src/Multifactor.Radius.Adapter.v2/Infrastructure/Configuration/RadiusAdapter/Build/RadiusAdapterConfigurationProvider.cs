//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using Multifactor.Radius.Adapter.v2.Infrastructure.Configuration.XmlAppConfiguration;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Configuration.RadiusAdapter.Build;

internal static class RadiusAdapterConfigurationProvider
{
    private static readonly Lazy<RadiusAdapterConfiguration> _rootConfig = new(() =>
    {
        var path = RadiusAdapterConfigurationFile.Path;
        var rdsRootConfig = new RadiusConfigurationFile(path);
        
        // try to read a file...
        if (File.Exists(rdsRootConfig))
        {
            return RadiusAdapterConfigurationFactory.Create(rdsRootConfig);
        }

        // ... and try to read an environment variables otherwise.
        return RadiusAdapterConfigurationFactory.Create();
    });

    public static RadiusAdapterConfiguration GetRootConfiguration() => _rootConfig.Value;
}
