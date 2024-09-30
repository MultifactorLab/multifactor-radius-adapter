//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using MultiFactor.Radius.Adapter.Infrastructure.Configuration.Exceptions;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration.Features.RandomWaiterFeature;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration.Models;
using NetTools;
using System;
using System.Net;
using System.Threading;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration.RootLevel;

namespace MultiFactor.Radius.Adapter.Infrastructure.Configuration.ConfigurationLoading;

public class ServiceConfigurationFactory
{
    private readonly IClientConfigurationsProvider _clientConfigurationsProvider;
    private readonly ClientConfigurationFactory _clientConfigFactory;

    public ServiceConfigurationFactory(IClientConfigurationsProvider clientConfigurationsProvider,
        ClientConfigurationFactory clientConfigFactory)
    {
        _clientConfigurationsProvider = clientConfigurationsProvider ?? throw new ArgumentNullException(nameof(clientConfigurationsProvider));
        _clientConfigFactory = clientConfigFactory ?? throw new ArgumentNullException(nameof(clientConfigFactory));
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


        if (string.IsNullOrEmpty(apiUrlSetting))
        {
            throw InvalidConfigurationException.For(x => x.AppSettings.MultifactorApiUrl,
                "'{prop}' element not found. Config name: '{0}'",
                RootConfigurationFile.ConfigName);
        }

        IPEndPoint serviceServerEndpoint = ParseAdapterServerEndpoint(appSettings);
        TimeSpan apiTimeout = ParseHttpTimeout(apiTimeoutSetting);

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
            var generalClient = _clientConfigFactory.CreateConfig(RootConfigurationFile.ConfigName, rootConfiguration, builder);
            builder.AddClient(IPAddress.Any, generalClient).IsSingleClientMode(true);
            return builder;
        }

        foreach (var clientConfig in clientConfigs)
        {
            var source = _clientConfigurationsProvider.GetSource(clientConfig);
            var client = _clientConfigFactory.CreateConfig(source.Name, clientConfig, builder);

            var clientSettings = clientConfig.AppSettings;
            var radiusClientNasIdentifierSetting = clientSettings.RadiusClientNasIdentifier;
            var radiusClientIpSetting = clientSettings.RadiusClientIp;

            if (!string.IsNullOrEmpty(radiusClientNasIdentifierSetting))
            {
                builder.AddClient(radiusClientNasIdentifierSetting, client);
                continue;
            }

            if (string.IsNullOrEmpty(radiusClientIpSetting))
            {
                throw InvalidConfigurationException.For(x => x.AppSettings.RadiusClientNasIdentifier,
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

        return builder;
    }

    private static TimeSpan ParseHttpTimeout(string mfTimeoutSetting)
    {
        var minimalApiTimeout = TimeSpan.FromSeconds(65);

        if (!TimeSpan.TryParseExact(mfTimeoutSetting, @"hh\:mm\:ss", null, System.Globalization.TimeSpanStyles.None, out var httpRequestTimeout))
            return minimalApiTimeout;

        return httpRequestTimeout == TimeSpan.Zero ?
            Timeout.InfiniteTimeSpan // infinity timeout
            : httpRequestTimeout < minimalApiTimeout
                ? minimalApiTimeout  // minimal timeout
                : httpRequestTimeout; // timeout from config
    }

    private static IPEndPoint ParseAdapterServerEndpoint(AppSettingsSection appSettings)
    {
        if (string.IsNullOrEmpty(appSettings.AdapterServerEndpoint))
        {
            throw InvalidConfigurationException.For(x => x.AppSettings.AdapterServerEndpoint,
                "'{prop}' element not found. Config name: '{0}'",
                RootConfigurationFile.ConfigName);
        }

        if (!IPEndPointFactory.TryParse(appSettings.AdapterServerEndpoint, out var serviceServerEndpoint))
        {
            throw InvalidConfigurationException.For(x => x.AppSettings.AdapterServerEndpoint,
                "Can't parse '{prop}' value. Config name: '{0}'",
                RootConfigurationFile.ConfigName);
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
            throw InvalidConfigurationException.For(x => x.AppSettings.InvalidCredentialDelay,
                "Can't parse '{prop}' value. Config name: '{0}'",
                RootConfigurationFile.ConfigName);            
        }
    }
}
