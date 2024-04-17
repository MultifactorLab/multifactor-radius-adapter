//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using Microsoft.Extensions.Logging;
using MultiFactor.Radius.Adapter.Configuration.Core;
using MultiFactor.Radius.Adapter.Configuration.Features.RandomWaiterFeature;
using MultiFactor.Radius.Adapter.Core;
using MultiFactor.Radius.Adapter.Core.Exceptions;
using NetTools;
using System;
using System.Configuration;
using System.IO;
using System.Net;
using System.Threading;

namespace MultiFactor.Radius.Adapter.Configuration.ConfigurationLoading;

public class ServiceConfigurationFactory
{
    private readonly IRootConfigurationProvider _appConfigurationProvider;
    private readonly IClientConfigurationsProvider _clientConfigurationsProvider;
    private readonly ClientConfigurationFactory _clientConfigFactory;
    private readonly ILogger<ServiceConfigurationFactory> _logger;

    public ServiceConfigurationFactory(IRootConfigurationProvider appConfigurationProvider, 
        IClientConfigurationsProvider clientConfigurationsProvider,
        ClientConfigurationFactory clientConfigFactory, 
        ILogger<ServiceConfigurationFactory> logger)
    {
        _appConfigurationProvider = appConfigurationProvider ?? throw new ArgumentNullException(nameof(appConfigurationProvider));
        _clientConfigurationsProvider = clientConfigurationsProvider ?? throw new ArgumentNullException(nameof(clientConfigurationsProvider));
        _clientConfigFactory = clientConfigFactory ?? throw new ArgumentNullException(nameof(clientConfigFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public IServiceConfiguration CreateConfig()
    {
        var rootConfig = _appConfigurationProvider.GetRootConfiguration();
        var appsettings = rootConfig.AppSettings.Settings;

        var serviceServerEndpointSetting = appsettings[Literals.Configuration.AdapterServerEndpoint]?.Value;
        var apiUrlSetting = appsettings[Literals.Configuration.MultifactorApiUrl]?.Value;
        var apiProxySetting = appsettings[Literals.Configuration.MultifactorApiProxy]?.Value;
        var apiTimeoutSetting = appsettings[Literals.Configuration.MultifactorApiTimeout]?.Value;

        if (string.IsNullOrEmpty(serviceServerEndpointSetting))
        {
            throw new InvalidConfigurationException($"'{Literals.Configuration.AdapterServerEndpoint}' element not found");
        }
        if (!IPEndPointFactory.TryParse(serviceServerEndpointSetting, out var serviceServerEndpoint))
        {
            throw new InvalidConfigurationException($"Can't parse '{Literals.Configuration.AdapterServerEndpoint}' value");
        }
        if (string.IsNullOrEmpty(apiUrlSetting))
        {
            throw new InvalidConfigurationException($"'{Literals.Configuration.MultifactorApiUrl}' element not found");
        }
        TimeSpan apiTimeout = ParseHttpTimeout(apiTimeoutSetting);

        var builder = new ServiceConfiguration()
            .SetServiceServerEndpoint(serviceServerEndpoint)
            .SetApiUrl(apiUrlSetting)
            .SetApiTimeout(apiTimeout);

        if (!string.IsNullOrWhiteSpace(apiProxySetting))
        {
            builder.SetApiProxy(apiProxySetting);
        }

        ReadInvalidCredDelaySetting(appsettings, builder);

        var clientConfigs = _clientConfigurationsProvider.GetClientConfigurations();
        if (clientConfigs.Length == 0)
        {
            // check if we have anything
            _ = (appsettings[Literals.Configuration.FirstFactorAuthSource]?.Value)
                ?? throw new InvalidConfigurationException("No clients' config files found. Use one of the *.template files in the /clients folder to customize settings. Then save this file as *.config.");
            var client = _clientConfigFactory.CreateConfig("General", rootConfig, builder);
            builder.AddClient(IPAddress.Any, client).IsSingleClientMode(true);

            return builder;
        }

        foreach (var clientConfig in clientConfigs)
        {
            var client = _clientConfigFactory.CreateConfig(Path.GetFileNameWithoutExtension(clientConfig.FilePath), clientConfig, builder);

            var clientSettings = clientConfig.AppSettings;
            var radiusClientNasIdentifierSetting = clientSettings.Settings[Literals.Configuration.RadiusClientNasIdentifier]?.Value;
            var radiusClientIpSetting = clientSettings.Settings[Literals.Configuration.RadiusClientIp]?.Value;

            if (!string.IsNullOrEmpty(radiusClientNasIdentifierSetting))
            {
                builder.AddClient(radiusClientNasIdentifierSetting, client);
                continue;
            }

            if (string.IsNullOrEmpty(radiusClientIpSetting))
            {
                throw new InvalidConfigurationException($"Either '{Literals.Configuration.RadiusClientNasIdentifier}' or '{Literals.Configuration.RadiusClientIp}' must be configured");
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

    private static void ReadInvalidCredDelaySetting(KeyValueConfigurationCollection appsettings, ServiceConfiguration builder)
    {
        try
        {
            var waiterConfig = RandomWaiterConfig.Create(appsettings[Literals.Configuration.InvalidCredentialDelay]?.Value);
            builder.SetInvalidCredentialDelay(waiterConfig);
        }
        catch
        {
            throw new InvalidConfigurationException($"Can't parse '{Literals.Configuration.InvalidCredentialDelay}' value");
        }
    }

    private static TimeSpan ParseHttpTimeout(string mfTimeoutSetting)
    {
        TimeSpan _minimalApiTimeout = TimeSpan.FromSeconds(65);

        if (!TimeSpan.TryParseExact(mfTimeoutSetting, @"hh\:mm\:ss", null, System.Globalization.TimeSpanStyles.None, out var httpRequestTimeout))
            return _minimalApiTimeout;

        return httpRequestTimeout == TimeSpan.Zero ?
            Timeout.InfiniteTimeSpan // infinity timeout
            : httpRequestTimeout < _minimalApiTimeout
                ? _minimalApiTimeout  // minimal timeout
                : httpRequestTimeout; // timeout from config
    }
}
