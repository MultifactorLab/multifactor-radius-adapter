using Microsoft.Extensions.Options;
using MultiFactor.Radius.Adapter.Configuration.ConfigurationLoading;
using System.Configuration;

namespace MultiFactor.Radius.Adapter.Tests.Fixtures.ConfigLoading
{
    internal class TestClientConfigsProvider : IClientConfigurationsProvider
    {
        private readonly TestConfigProviderOptions _options;

        public TestClientConfigsProvider(IOptions<TestConfigProviderOptions> options)
        {
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        }

        public System.Configuration.Configuration[] GetClientConfigurations()
        {
            var clientConfigFilesPath = GetFolderPath();
            var clientConfigFiles = Directory.Exists(clientConfigFilesPath) 
                ? Directory.GetFiles(clientConfigFilesPath, "*.config") 
                : Array.Empty<string>();
            if (clientConfigFiles.Length == 0) return Array.Empty<System.Configuration.Configuration>();

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

        private string GetFolderPath()
        {
            if (!string.IsNullOrWhiteSpace(_options.ClientConfigsFolderPath))
            {
                return _options.ClientConfigsFolderPath;
            }
            return "clients";
        }
    }
}
