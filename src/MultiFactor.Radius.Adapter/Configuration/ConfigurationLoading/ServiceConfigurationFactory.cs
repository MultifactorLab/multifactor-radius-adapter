//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using MultiFactor.Radius.Adapter.Configuration.Core;
using MultiFactor.Radius.Adapter.Configuration.Features.RadiusReplyAttributeFeature;
using MultiFactor.Radius.Adapter.Configuration.Features.RandomWaiterFeature;
using MultiFactor.Radius.Adapter.Configuration.Features.UserNameTransformFeature;
using MultiFactor.Radius.Adapter.Core;
using MultiFactor.Radius.Adapter.Core.Exceptions;
using MultiFactor.Radius.Adapter.Core.Radius.Attributes;
using NetTools;
using Serilog;
using System;
using System.Collections;
using System.Configuration;
using System.IO;
using System.Net;

namespace MultiFactor.Radius.Adapter.Configuration.ConfigurationLoading
{

    public class ServiceConfigurationFactory
    {
        private readonly IRootConfigurationProvider _appConfigurationProvider;
        private readonly IClientConfigurationsProvider _clientConfigurationsProvider;
        private readonly IRadiusDictionary _dictionary;
        private readonly ClientConfigurationFactory _clientConfigFactory;
        private readonly ILogger _logger;

        public ServiceConfigurationFactory(IRootConfigurationProvider appConfigurationProvider, 
            IClientConfigurationsProvider clientConfigurationsProvider,
            IRadiusDictionary dictionary, 
            ClientConfigurationFactory clientConfigFactory, 
            ILogger logger)
        {
            _appConfigurationProvider = appConfigurationProvider ?? throw new ArgumentNullException(nameof(appConfigurationProvider));
            _clientConfigurationsProvider = clientConfigurationsProvider ?? throw new ArgumentNullException(nameof(clientConfigurationsProvider));
            _dictionary = dictionary ?? throw new ArgumentNullException(nameof(dictionary));
            _clientConfigFactory = clientConfigFactory ?? throw new ArgumentNullException(nameof(clientConfigFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public IServiceConfiguration CreateConfig()
        {
            var rootConfig = _appConfigurationProvider.GetRootConfiguration();

            var serviceServerEndpointSetting = rootConfig.AppSettings.Settings["adapter-server-endpoint"]?.Value;
            var apiUrlSetting = rootConfig.AppSettings.Settings["multifactor-api-url"]?.Value;
            var apiProxySetting = rootConfig.AppSettings.Settings[Literals.Configuration.MultifactorApiProxy]?.Value;

            if (string.IsNullOrEmpty(serviceServerEndpointSetting))
            {
                throw new InvalidConfigurationException("'adapter-server-endpoint' element not found");
            }
            if (string.IsNullOrEmpty(apiUrlSetting))
            {
                throw new InvalidConfigurationException("'multifactor-api-url' element not found");
            }
            if (!IPEndPointFactory.TryParse(serviceServerEndpointSetting, out var serviceServerEndpoint))
            {
                throw new InvalidConfigurationException("Can't parse 'adapter-server-endpoint' value");
            }

            var builder = ServiceConfiguration.CreateBuilder()
                .SetServiceServerEndpoint(serviceServerEndpoint)
                .SetApiUrl(apiUrlSetting)
                .SetApiProxy(apiProxySetting);

            try
            {
                var waiterConfig = RandomWaiterConfig.Create(rootConfig.AppSettings.Settings[Literals.Configuration.PciDss.InvalidCredentialDelay]?.Value);
                builder.SetInvalidCredentialDelay(waiterConfig);
            }
            catch
            {
                throw new InvalidConfigurationException($"Can't parse '{Literals.Configuration.PciDss.InvalidCredentialDelay}' value");
            }

            var clientConfigs = _clientConfigurationsProvider.GetClientConfigurations();
            if (clientConfigs.Length == 0)
            {
                //check if we have anything
                var ffas = rootConfig.AppSettings.Settings["first-factor-authentication-source"]?.Value;
                if (ffas == null)
                {
                    throw new InvalidConfigurationException("No clients' config files found. Use one of the *.template files in the /clients folder to customize settings. Then save this file as *.config.");
                }

                var radiusReplyAttributesSection = rootConfig.GetSection("RadiusReply") as RadiusReplyAttributesSection;
                var userNameTransformRulesSection = rootConfig.GetSection("UserNameTransformRules") as UserNameTransformRulesSection;

                var client = _clientConfigFactory.CreateConfig("General",
                    rootConfig.AppSettings,
                    radiusReplyAttributesSection,
                    userNameTransformRulesSection);
                builder.AddClient(IPAddress.Any, client).IsSingleClientMode(true);

                return builder.Build();
            }
                   
            foreach (var clientConfig in clientConfigs)
            {
                var clientSettings = (AppSettingsSection)clientConfig.GetSection("appSettings");
                var radiusReplyAttributesSection = clientConfig.GetSection("RadiusReply") as RadiusReplyAttributesSection;
                var userNameTransformRulesSection = clientConfig.GetSection("UserNameTransformRules") as UserNameTransformRulesSection;

                var client = _clientConfigFactory.CreateConfig(Path.GetFileNameWithoutExtension(clientConfig.FilePath), 
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
                    throw new InvalidConfigurationException("Either 'radius-client-nas-identifier' or 'radius-client-ip' must be configured");
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