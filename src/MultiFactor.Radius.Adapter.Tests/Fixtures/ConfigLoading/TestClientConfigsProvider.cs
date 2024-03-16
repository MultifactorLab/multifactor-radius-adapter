using Microsoft.Extensions.Options;
using MultiFactor.Radius.Adapter.Configuration.Core;
using System.Configuration;

namespace MultiFactor.Radius.Adapter.Tests.Fixtures.ConfigLoading;

internal class TestClientConfigsProvider : IClientConfigurationsProvider
{
    private readonly TestConfigProviderOptions _options;

    public TestClientConfigsProvider(IOptions<TestConfigProviderOptions> options)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    public System.Configuration.Configuration[] GetClientConfigurations()
    {
        var clientConfigFiles = GetFiles().ToArray();
        if (clientConfigFiles.Length == 0)
        {
            return Array.Empty<System.Configuration.Configuration>();
        }

        var list = new List<System.Configuration.Configuration>();
        foreach (var file in clientConfigFiles)
        {
            var customConfigFileMap = new ExeConfigurationFileMap
            {
                ExeConfigFilename = file
            };
            list.Add(ConfigurationManager.OpenMappedExeConfiguration(customConfigFileMap, ConfigurationUserLevel.None));
        }

        return list.ToArray();
    }

    private IEnumerable<string> GetFiles()
    {
        if (_options.ClientConfigFilePaths != null && _options.ClientConfigFilePaths.Length != 0)
        {
            foreach (var f in _options.ClientConfigFilePaths)
            {
                if (File.Exists(f))
                {
                    yield return f;
                }
            }

            yield break;
        }

        if (string.IsNullOrWhiteSpace(_options.ClientConfigsFolderPath))
        {
            yield break;
        }

        if (!Directory.Exists(_options.ClientConfigsFolderPath))
        {
            yield break;
        }

        foreach (var f in Directory.GetFiles(_options.ClientConfigsFolderPath, "*.config"))
        {
            yield return f;
        }
    }
}
