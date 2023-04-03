//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using MultiFactor.Radius.Adapter.Configuration.Core;
using MultiFactor.Radius.Adapter.Configuration.Features.RadiusReplyAttributeFeature;
using MultiFactor.Radius.Adapter.Configuration.Features.RandomWaiterFeature;
using MultiFactor.Radius.Adapter.Configuration.Features.UserNameTransformFeature;
using MultiFactor.Radius.Adapter.Core;
using MultiFactor.Radius.Adapter.Core.Radius.Attributes;
using NetTools;
using Serilog;
using System;
using System.Configuration;
using System.IO;
using System.Net;

namespace MultiFactor.Radius.Adapter.Configuration.ConfigurationLoading
{
    public class ServiceConfigurationFactory
    {
        private readonly IAppConfigurationProvider _appConfigurationProvider;
        private readonly IRadiusDictionary _dictionary;
        private readonly ClientConfigurationFactory _clientConfigFactory;
        private readonly ILogger _logger;

        public ServiceConfigurationFactory(IAppConfigurationProvider appConfigurationProvider, IRadiusDictionary dictionary, ClientConfigurationFactory clientConfigFactory, ILogger logger)
        {
            _appConfigurationProvider = appConfigurationProvider ?? throw new ArgumentNullException(nameof(appConfigurationProvider));
            _dictionary = dictionary ?? throw new ArgumentNullException(nameof(dictionary));
            _clientConfigFactory = clientConfigFactory ?? throw new ArgumentNullException(nameof(clientConfigFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public IServiceConfiguration CreateConfig()
        {
            var rootConfig = _appConfigurationProvider.GetRootConfiguration();

            var appSettingsSection = rootConfig.GetSection("appSettings");
            var appSettings = appSettingsSection as AppSettingsSection;

            var serviceServerEndpointSetting = appSettings.Settings["adapter-server-endpoint"]?.Value;
            var apiUrlSetting = appSettings.Settings["multifactor-api-url"]?.Value;
            var apiProxySetting = appSettings.Settings[Literals.Configuration.MultifactorApiProxy]?.Value;

            if (string.IsNullOrEmpty(serviceServerEndpointSetting))
            {
                throw new Exception("Configuration error: 'adapter-server-endpoint' element not found");
            }
            if (string.IsNullOrEmpty(apiUrlSetting))
            {
                throw new Exception("Configuration error: 'multifactor-api-url' element not found");
            }
            if (!IPEndPointFactory.TryParse(serviceServerEndpointSetting, out var serviceServerEndpoint))
            {
                throw new Exception("Configuration error: Can't parse 'adapter-server-endpoint' value");
            }

            var builder = ServiceConfiguration.CreateBuilder()
                .SetServiceServerEndpoint(serviceServerEndpoint)
                .SetApiUrl(apiUrlSetting)
                .SetApiProxy(apiProxySetting);

            try
            {
                var waiterConfig = RandomWaiterConfig.Create(appSettings.Settings[Literals.Configuration.PciDss.InvalidCredentialDelay]?.Value);
                builder.SetInvalidCredentialDelay(waiterConfig);
            }
            catch
            {
                throw new Exception($"Configuration error: Can't parse '{Literals.Configuration.PciDss.InvalidCredentialDelay}' value");
            }

            var clientConfigFilesPath = $"{Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory)}{Path.DirectorySeparatorChar}clients";
            var clientConfigFiles = Directory.Exists(clientConfigFilesPath) ? Directory.GetFiles(clientConfigFilesPath, "*.config") : new string[0];

            if (clientConfigFiles.Length == 0)
            {
                //check if we have anything
                var ffas = appSettings.Settings["first-factor-authentication-source"]?.Value;
                if (ffas == null)
                {
                    throw new ConfigurationErrorsException("No clients' config files found. Use one of the *.template files in the /clients folder to customize settings. Then save this file as *.config.");
                }

                var radiusReplyAttributesSection = ConfigurationManager.GetSection("RadiusReply") as RadiusReplyAttributesSection;
                var userNameTransformRulesSection = ConfigurationManager.GetSection("UserNameTransformRules") as UserNameTransformRulesSection;

                var client = _clientConfigFactory.CreateConfig("General", 
                    appSettings, 
                    radiusReplyAttributesSection, 
                    userNameTransformRulesSection);
                builder.AddClient(IPAddress.Any, client).IsSingleClientMode(true);

                return builder.Build();
            }
                   
            foreach (var clientConfigFile in clientConfigFiles)
            {
                _logger.Information($"Loading client configuration from {Path.GetFileName(clientConfigFile)}");

                var clientConfig = _appConfigurationProvider.GetClientConfiguration(clientConfigFile);
                var clientSettings = (AppSettingsSection)clientConfig.GetSection("appSettings");
                var radiusReplyAttributesSection = clientConfig.GetSection("RadiusReply") as RadiusReplyAttributesSection;
                var userNameTransformRulesSection = clientConfig.GetSection("UserNameTransformRules") as UserNameTransformRulesSection;

                var client = _clientConfigFactory.CreateConfig(Path.GetFileNameWithoutExtension(clientConfigFile), 
                    clientSettings, 
                    radiusReplyAttributesSection, 
                    userNameTransformRulesSection);

                var radiusClientNasIdentifierSetting = clientSettings.Settings["radius-client-nas-identifier"]?.Value;
                var radiusClientIpSetting = clientSettings.Settings["radius-client-ip"]?.Value;

                if (!string.IsNullOrEmpty(radiusClientNasIdentifierSetting))
                {
                    builder.AddClient(radiusClientNasIdentifierSetting, client);
                    continue;
                }

                if (string.IsNullOrEmpty(radiusClientIpSetting))
                {
                    throw new Exception("Configuration error: Either 'radius-client-nas-identifier' or 'radius-client-ip' must be configured");
                }

                var elements = radiusClientIpSetting.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var element in elements)
                {
                    foreach (var ip in IPAddressRange.Parse(element))
                    {
                        builder.AddClient(ip, client);
                    }
                }
            }
            
            return builder.Build();
        }  
    }
}