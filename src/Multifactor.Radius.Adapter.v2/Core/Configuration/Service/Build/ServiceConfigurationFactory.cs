//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using System.Net;
using Microsoft.Extensions.Logging;
using Multifactor.Radius.Adapter.v2.Core.Configuration.Client.Build;
using Multifactor.Radius.Adapter.v2.Core.RandomWaiterFeature;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configuration;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configuration.Exceptions;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configuration.RadiusAdapter;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configuration.RadiusAdapter.Sections;
using NetTools;

namespace Multifactor.Radius.Adapter.v2.Core.Configuration.Service.Build;

public class ServiceConfigurationFactory : IServiceConfigurationFactory
{
    private readonly IClientConfigurationsProvider _clientConfigurationsProvider;
    private readonly IClientConfigurationFactory _clientConfigFactoryLdapSettings;
    private readonly ILogger<IServiceConfigurationFactory> _logger;
    private static readonly TimeSpan RecommendedMinimalApiTimeout = TimeSpan.FromSeconds(65);

    public ServiceConfigurationFactory(
        IClientConfigurationsProvider clientConfigurationsProvider,
        IClientConfigurationFactory clientConfigFactoryLdapSettings,
        ILogger<IServiceConfigurationFactory> logger)
    {
        _clientConfigurationsProvider = clientConfigurationsProvider ?? throw new ArgumentNullException(nameof(clientConfigurationsProvider));
        _clientConfigFactoryLdapSettings = clientConfigFactoryLdapSettings ?? throw new ArgumentNullException(nameof(clientConfigFactoryLdapSettings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public IServiceConfiguration CreateConfig(RadiusAdapterConfiguration rootConfiguration)
    {
        if (rootConfiguration is null)
        {
            throw new ArgumentNullException(nameof(rootConfiguration));
        }

        var appSettings = rootConfiguration.AppSettings;

        var apiUrlSetting = appSettings.MultifactorApiUrl;
        var apiProxySetting = appSettings.MultifactorApiProxy;
        var apiTimeoutSetting = appSettings.MultifactorApiTimeout;
        
        if (string.IsNullOrWhiteSpace(apiUrlSetting))
        {
            throw InvalidConfigurationException.For(
                x => x.AppSettings.MultifactorApiUrl,
                "'{prop}' element not found. Config name: '{0}'",
                RadiusAdapterConfigurationFile.ConfigName);
        }

        IPEndPoint serviceServerEndpoint = ParseAdapterServerEndpoint(appSettings);
        
        TimeSpan apiTimeout = ParseMultifactorApiTimeout(apiTimeoutSetting,out var forcedTimeout);
        
        if (Timeout.InfiniteTimeSpan != apiTimeout && apiTimeout < RecommendedMinimalApiTimeout)
        {
            if (forcedTimeout)
            {
                _logger.LogWarning(
                    "You have set the timeout to {httpRequestTimeout} seconds. The recommended minimal timeout is {recommendedApiTimeout} seconds. Lowering this threshold may cause incorrect system behavior.",
                    apiTimeout.TotalSeconds,
                    RecommendedMinimalApiTimeout.TotalSeconds);
            }
            else
            {
                _logger.LogWarning(
                    "You have tried to set the timeout to {httpRequestTimeout} seconds. The recommended minimal timeout is {recommendedApiTimeout} seconds. If you are sure, use the following syntax: 'value={apiTimeoutSetting}!'",
                    apiTimeout.TotalSeconds,
                    RecommendedMinimalApiTimeout.TotalSeconds,
                    apiTimeoutSetting);

                apiTimeout = RecommendedMinimalApiTimeout;
            }
        }
        
        var builder = new ServiceConfiguration()
            .SetServiceServerEndpoint(serviceServerEndpoint)
            .SetApiUrl(apiUrlSetting)
            .SetApiTimeout(apiTimeout);

        if (!string.IsNullOrWhiteSpace(apiProxySetting))
        {
            builder.SetApiProxy(apiProxySetting);
        }

        ReadInvalidCredDelaySetting(appSettings, builder);

        var clientConfigs = _clientConfigurationsProvider.GetClientConfigurations();
        if (clientConfigs.Length == 0)
        {
            var generalClient = _clientConfigFactoryLdapSettings.CreateConfig(RadiusAdapterConfigurationFile.ConfigName, rootConfiguration, builder);
            builder.AddClient(IPAddress.Any, generalClient).IsSingleClientMode(true);
            return builder;
        }

        foreach (var clientConfig in clientConfigs)
        {
            AddClient(clientConfig, builder);
        }

        return builder;
    }

    private void AddClient(RadiusAdapterConfiguration clientConfig, ServiceConfiguration builder)
    {
        var source = _clientConfigurationsProvider.GetSource(clientConfig);
        var client = _clientConfigFactoryLdapSettings.CreateConfig(source.Name, clientConfig, builder);

        var clientSettings = clientConfig.AppSettings;
        var radiusClientNasIdentifierSetting = clientSettings.RadiusClientNasIdentifier;
        var radiusClientIpSetting = clientSettings.RadiusClientIp;

        if (!string.IsNullOrWhiteSpace(radiusClientNasIdentifierSetting))
        {
            builder.AddClient(radiusClientNasIdentifierSetting, client);
            return;
        }

        if (string.IsNullOrWhiteSpace(radiusClientIpSetting))
        {
            throw InvalidConfigurationException.For(
                x => x.AppSettings.RadiusClientNasIdentifier,
                "Either '{prop}' or '{0}' must be configured. Config name: '{1}'",
                RadiusAdapterConfigurationDescription.Property(x => x.AppSettings.RadiusClientIp),
                client.Name);
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

    private TimeSpan ParseMultifactorApiTimeout(string mfTimeoutSetting, out bool forcedTimeout)
    {
        forcedTimeout = IsForcedTimeout(mfTimeoutSetting);
        if (forcedTimeout)
        {
            mfTimeoutSetting = mfTimeoutSetting.TrimEnd('!');
        }
        
        if (!TimeSpan.TryParseExact(mfTimeoutSetting, @"hh\:mm\:ss", null, System.Globalization.TimeSpanStyles.None, out var httpRequestTimeout))
            return RecommendedMinimalApiTimeout;

        if (httpRequestTimeout == TimeSpan.Zero)
            return Timeout.InfiniteTimeSpan;
        
        return httpRequestTimeout;
    }

    private bool IsForcedTimeout(string mfTimeoutSetting) => mfTimeoutSetting?.EndsWith("!") ?? false;

    private static IPEndPoint ParseAdapterServerEndpoint(AppSettingsSection appSettings)
    {
        if (string.IsNullOrWhiteSpace(appSettings.AdapterServerEndpoint))
        {
            throw InvalidConfigurationException.For(
                x => x.AppSettings.AdapterServerEndpoint,
                "'{prop}' element not found. Config name: '{0}'",
                RadiusAdapterConfigurationFile.ConfigName);
        }

        if (!IPEndPointFactory.TryParse(appSettings.AdapterServerEndpoint, out var serviceServerEndpoint))
        {
            throw InvalidConfigurationException.For(
                x => x.AppSettings.AdapterServerEndpoint,
                "Can't parse '{prop}' value. Config name: '{0}'",
                RadiusAdapterConfigurationFile.ConfigName);
        }

        return serviceServerEndpoint;
    }

    private static void ReadInvalidCredDelaySetting(AppSettingsSection appSettings, ServiceConfiguration builder)
    {
        try
        {
            var waiterConfig = RandomWaiterConfig.Create(appSettings.InvalidCredentialDelay);
            builder.SetInvalidCredentialDelay(waiterConfig);
        }
        catch
        {
            throw InvalidConfigurationException.For(
                x => x.AppSettings.InvalidCredentialDelay,
                "Can't parse '{prop}' value. Config name: '{0}'",
                RadiusAdapterConfigurationFile.ConfigName);            
        }
    }
}
