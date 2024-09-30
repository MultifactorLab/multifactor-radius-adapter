using Microsoft.Extensions.Options;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration.ConfigurationLoading;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration.Models;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration.XmlAppConfiguration;

namespace MultiFactor.Radius.Adapter.Tests.Fixtures.ConfigLoading;

internal class TestClientConfigsProvider : IClientConfigurationsProvider
{
    private Dictionary<RadiusConfigurationFile, RadiusAdapterConfiguration> _dict = new();
    private readonly TestConfigProviderOptions _options;

    public TestClientConfigsProvider(IOptions<TestConfigProviderOptions> options)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    public RadiusAdapterConfiguration[] GetClientConfigurations()
    {
        var clientConfigFiles = GetFiles().ToArray();
        if (clientConfigFiles.Length == 0)
        {
            return Array.Empty<RadiusAdapterConfiguration>();
        }

        _dict = clientConfigFiles
            .Select(x => new RadiusConfigurationFile(x))
            .ToDictionary(k => k, v => RadiusAdapterConfigurationFactory.Create(v, v.Name));

        return _dict.Select(x => x.Value).ToArray();
    }

    public RadiusConfigurationSource GetSource(RadiusAdapterConfiguration configuration)
    {
        return _dict.FirstOrDefault(x => x.Value == configuration).Key;
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
