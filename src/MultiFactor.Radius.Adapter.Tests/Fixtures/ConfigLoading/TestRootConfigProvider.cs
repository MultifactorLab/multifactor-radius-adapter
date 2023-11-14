using Microsoft.Extensions.Options;
using MultiFactor.Radius.Adapter.Configuration.Core;
using System.Configuration;

namespace MultiFactor.Radius.Adapter.Tests.Fixtures.ConfigLoading;

internal class TestRootConfigProvider : IRootConfigurationProvider
{
    private readonly TestConfigProviderOptions _options;

    public TestRootConfigProvider(IOptions<TestConfigProviderOptions> options)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    public System.Configuration.Configuration GetRootConfiguration()
    {
        if (string.IsNullOrWhiteSpace(_options.RootConfigFilePath))
        {
            return ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
        }

        var customConfigFileMap = new ExeConfigurationFileMap
        {
            ExeConfigFilename = _options.RootConfigFilePath
        };
        return ConfigurationManager.OpenMappedExeConfiguration(customConfigFileMap, ConfigurationUserLevel.None);
    }
}
