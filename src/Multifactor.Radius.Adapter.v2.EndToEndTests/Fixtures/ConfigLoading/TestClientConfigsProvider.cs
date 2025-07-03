using Microsoft.Extensions.Options;
using Multifactor.Radius.Adapter.v2.Core.Configuration.Client.Build;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configuration.RadiusAdapter;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configuration.RadiusAdapter.Build;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configuration.XmlAppConfiguration;

namespace Multifactor.Radius.Adapter.v2.EndToEndTests.Fixtures.ConfigLoading;

internal class TestClientConfigsProvider(IOptions<TestConfigProviderOptions> options) : IClientConfigurationsProvider
{
    private readonly Dictionary<RadiusConfigurationSource, RadiusAdapterConfiguration> _dict = new();
    private readonly TestConfigProviderOptions _options = options?.Value ?? throw new ArgumentNullException(nameof(options));

    public RadiusAdapterConfiguration[] GetClientConfigurations()
    {
        var clientConfigFiles = GetFiles().ToArray();
        
        if (clientConfigFiles.Length == 0)
            return [];
        
        var fileSources = clientConfigFiles.Select(x => new RadiusConfigurationFile(x)).ToArray();
        foreach (var file in fileSources)
        {
            var config = RadiusAdapterConfigurationFactory.Create(file, file.Name);
            _dict.Add(file, config);
        }
        
        var envVarSources = DefaultClientConfigurationsProvider.GetEnvVarClients()
            .Select(x => new RadiusConfigurationEnvironmentVariable(x))
            .ExceptBy(fileSources.Select(x => RadiusConfigurationSource.TransformName(x.Name)), x => x.Name);
        
        foreach (var envVarClient in envVarSources)
        {
            var config = RadiusAdapterConfigurationFactory.Create(envVarClient);
            _dict.Add(envVarClient, config);
        }
        
        return _dict.Select(x => x.Value).ToArray();
    }

    public RadiusConfigurationSource GetSource(RadiusAdapterConfiguration configuration)
    {
        return _dict.FirstOrDefault(x => x.Value == configuration).Key;
    }

    private IEnumerable<string> GetFiles()
    {
        if (_options.ClientConfigFilePaths.Length > 0)
        {
            foreach (var f in _options.ClientConfigFilePaths)
            {
                if (File.Exists(f))
                    yield return f;
            }

            yield break;
        }

        if (string.IsNullOrWhiteSpace(_options.ClientConfigsFolderPath))
            yield break;

        if (!Directory.Exists(_options.ClientConfigsFolderPath))
            yield break;

        foreach (var f in Directory.GetFiles(_options.ClientConfigsFolderPath, "*.config"))
            yield return f;
    }
}
