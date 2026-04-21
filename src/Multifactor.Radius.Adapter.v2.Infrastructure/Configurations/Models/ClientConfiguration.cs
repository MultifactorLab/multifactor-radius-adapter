using System.Net;
using Multifactor.Radius.Adapter.v2.Application.Core.Enum;
using Multifactor.Radius.Adapter.v2.Application.Core.Models;
using Multifactor.Radius.Adapter.v2.Application.Core.Models.Abstractions;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configurations.Exceptions;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configurations.Parser;
using Multifactor.Radius.Adapter.v2.Infrastructure.Logging;
using NetTools;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Configurations.Models;

internal sealed class ClientConfiguration : IClientConfiguration
{
    public string Name { get; private init; }

    public string MultifactorNasIdentifier { get; private init; }
    public string MultifactorSharedSecret { get; private init; }
    public IReadOnlyList<string> SignUpGroups { get; private set; }
    public bool BypassSecondFactorWhenApiUnreachable { get; private init; }
    public AuthenticationSource FirstFactorAuthenticationSource { get; private set; }
    public IPEndPoint AdapterClientEndpoint { get; private set; }

    public IReadOnlyList<IPAddress>? RadiusClientIps { get; private set; }
    public IReadOnlyList<IPAddress>? RadiusClientNasIps { get; private set; }
    public string RadiusClientNasIdentifier { get; private set; }
    public string RadiusSharedSecret { get; private init; }
    public IReadOnlyList<IPEndPoint> NpsServerEndpoints { get; private set; }
    public TimeSpan NpsServerTimeout { get; private set; }

    public Privacy Privacy { get; private set; }

    public PreAuthMode? PreAuthenticationMethod { get; private set; } = PreAuthMode.None;
    public TimeSpan AuthenticationCacheLifetime { get; private set; } = TimeSpan.Zero;
    public CredentialDelay? InvalidCredentialDelay { get; private set; }
    public string? CallingStationIdAttribute { get; private init; }
    public bool IsIpFromUdp { get; private init; }  
    public IReadOnlyList<IPAddressRange> IpWhiteList { get; private set; }
    public bool IsAccessChallengePassword { get; private init; }


    public IReadOnlyList<ILdapServerConfiguration>? LdapServers { get; set; }
    public IReadOnlyDictionary<string, IReadOnlyList<IRadiusReplyAttribute>>? ReplyAttributes { get; set; }


    public static ClientConfiguration FromConfiguration(AdapterConfiguration configurationFile, bool isRoot)
    {
        ArgumentNullException.ThrowIfNull(configurationFile);
        const string formatedMessage = "Invalid '{prop}'. Value '{0}' cannot be parsed.";
        var dto = new ClientConfiguration
        {
            Name = configurationFile.FileName,
            MultifactorNasIdentifier =
                !string.IsNullOrWhiteSpace(configurationFile.AppSettings.MultifactorNasIdentifier)
                    ? configurationFile.AppSettings.MultifactorNasIdentifier
                    : throw InvalidConfigurationException.For(prop => prop.AppSettings.MultifactorNasIdentifier,
                        "Property '{prop}' is required. Config name: '{0}'", configurationFile.FileName),
            MultifactorSharedSecret = !string.IsNullOrWhiteSpace(configurationFile.AppSettings.MultifactorSharedSecret)
                ? configurationFile.AppSettings.MultifactorSharedSecret
                : throw InvalidConfigurationException.For(prop => prop.AppSettings.MultifactorSharedSecret,
                    "Property '{prop}' is required. Config name: '{0}'", configurationFile.FileName),
            RadiusSharedSecret = !string.IsNullOrWhiteSpace(configurationFile.AppSettings.RadiusSharedSecret)
                ? configurationFile.AppSettings.RadiusSharedSecret
                : throw InvalidConfigurationException.For(c => c.AppSettings.RadiusSharedSecret,
                    "Property '{prop}' is required. Config name: '{0}'", configurationFile.FileName),
            CallingStationIdAttribute = configurationFile.AppSettings.CallingStationIdAttribute,
            IsIpFromUdp = configurationFile.AppSettings.IpFromUdp ?? true,
            BypassSecondFactorWhenApiUnreachable = configurationFile.AppSettings.BypassSecondFactorWhenApiUnreachable,
            Privacy = new Privacy(PrivacyMode.None, []),
            PreAuthenticationMethod = PreAuthMode.None,
            AuthenticationCacheLifetime = TimeSpan.Zero,
            InvalidCredentialDelay = null,
            NpsServerEndpoints = [],
            NpsServerTimeout = TimeSpan.Parse("00:00:05"),
            SignUpGroups = [],
            RadiusClientIps = [],
            RadiusClientNasIps = [],
            IpWhiteList = [],
            IsAccessChallengePassword = configurationFile.AppSettings.AccessChallengePassword ?? true
        };
        if (!string.IsNullOrWhiteSpace(configurationFile.AppSettings.SignUpGroups))
            if (ConfigurationValueParser.TryParseStringList(configurationFile.AppSettings.SignUpGroups, out var list))
            {
                dto.SignUpGroups = list;
            }
            else
            {
                var exception = InvalidConfigurationException.For(c => c.AppSettings.SignUpGroups,
                    formatedMessage, configurationFile.AppSettings.SignUpGroups);
                StartupLogger.Warning(exception.Message);
                dto.SignUpGroups = [];
            }
        if (!isRoot)
        {
            int filledCount = 0;
            if (!string.IsNullOrEmpty(configurationFile.AppSettings.RadiusClientIp)) filledCount++;
            if (!string.IsNullOrEmpty(configurationFile.AppSettings.RadiusClientNasIp)) filledCount++;
            if (!string.IsNullOrEmpty(configurationFile.AppSettings.RadiusClientNasIdentifier)) filledCount++;

            switch (filledCount)
            {
                case 0:
                    throw new InvalidConfigurationException("Fields 'radius-client-ip', 'radius-client-nas-ip' or 'radius-client-nas-identifier' is required");
                case > 1:
                    throw new InvalidConfigurationException("Use only one of 'radius-client-ip', 'radius-client-nas-ip' or 'radius-client-nas-identifier'");
            }

            if (!string.IsNullOrWhiteSpace(configurationFile.AppSettings.RadiusClientIp))
                if (ConfigurationValueParser.TryParseIpAddress(configurationFile.AppSettings.RadiusClientIp,
                        out var address))
                {
                    dto.RadiusClientIps = address;
                }
                else
                {
                    var exception = InvalidConfigurationException.For(c => c.AppSettings.RadiusClientIp,
                        formatedMessage, configurationFile.AppSettings.RadiusClientIp);
                    StartupLogger.Warning(exception.Message);
                    dto.RadiusClientIps = [];
                }
            if (!string.IsNullOrWhiteSpace(configurationFile.AppSettings.RadiusClientNasIp))
                if (ConfigurationValueParser.TryParseIpAddress(configurationFile.AppSettings.RadiusClientNasIp,
                        out var address))
                {
                    dto.RadiusClientNasIps = address;
                }
                else
                {
                    var exception = InvalidConfigurationException.For(c => c.AppSettings.RadiusClientNasIp,
                        formatedMessage, configurationFile.AppSettings.RadiusClientNasIp);
                    StartupLogger.Warning(exception.Message);
                    dto.RadiusClientNasIps = [];
                }

            dto.RadiusClientNasIdentifier = configurationFile.AppSettings.RadiusClientNasIdentifier;
        }

        if (!string.IsNullOrWhiteSpace(configurationFile.AppSettings.NpsServerEndpoint))
            if (ConfigurationValueParser.TryParseEndpoints(configurationFile.AppSettings.NpsServerEndpoint,
                    out var npsServerEndpoints))
            {
                dto.NpsServerEndpoints = npsServerEndpoints;
            }
            else
            {
                var exception = InvalidConfigurationException.For(c => c.AppSettings.NpsServerEndpoint,
                    formatedMessage, configurationFile.AppSettings.NpsServerEndpoint);
                StartupLogger.Warning(exception.Message);
                dto.NpsServerEndpoints = [];
            }

        if (!string.IsNullOrWhiteSpace(configurationFile.AppSettings.NpsServerTimeout))
            if (ConfigurationValueParser.TryParseTimeout(configurationFile.AppSettings.NpsServerTimeout,
                    out var timeout))
            {
                dto.NpsServerTimeout = timeout.Value;
            }
            else
            {
                dto.NpsServerTimeout = TimeSpan.Parse("00:00:05");
                var exception = InvalidConfigurationException.For(c => c.AppSettings.NpsServerTimeout,
                    formatedMessage, configurationFile.AppSettings.NpsServerTimeout);
                StartupLogger.Warning(exception.Message);
            }

        if (!string.IsNullOrWhiteSpace(configurationFile.AppSettings.PrivacyMode))
            if (ConfigurationValueParser.TryParsePrivacyModeWithFields(configurationFile.AppSettings.PrivacyMode,
                    out var privacy))
            {
                dto.Privacy = privacy;
            }
            else
            {
                dto.Privacy = new Privacy(PrivacyMode.None, []);
                var exception = InvalidConfigurationException.For(c => c.AppSettings.PrivacyMode,
                    formatedMessage, configurationFile.AppSettings.PrivacyMode);
                StartupLogger.Warning(exception.Message);
            }

        if (!string.IsNullOrWhiteSpace(configurationFile.AppSettings.PreAuthenticationMethod))
            if (ConfigurationValueParser.TryParseEnum<PreAuthMode>(
                    configurationFile.AppSettings.PreAuthenticationMethod,
                    out var mode))
            {
                dto.PreAuthenticationMethod = mode;
            }
            else
            {
                dto.PreAuthenticationMethod = PreAuthMode.None;
                var exception = InvalidConfigurationException.For(c => c.AppSettings.PreAuthenticationMethod,
                    formatedMessage, configurationFile.AppSettings.PreAuthenticationMethod);
                StartupLogger.Warning(exception.Message);
            }

        if (!string.IsNullOrWhiteSpace(configurationFile.AppSettings.AuthenticationCacheLifetime))
            if (ConfigurationValueParser.TryParseTimeSpan(configurationFile.AppSettings.AuthenticationCacheLifetime,
                    out var span))
            {
                dto.AuthenticationCacheLifetime = span;
            }
            else
            {
                dto.AuthenticationCacheLifetime = TimeSpan.Zero;
                var exception = InvalidConfigurationException.For(c => c.AppSettings.AuthenticationCacheLifetime,
                    formatedMessage, configurationFile.AppSettings.AuthenticationCacheLifetime);
                StartupLogger.Warning(exception.Message);
            }

        if (!string.IsNullOrWhiteSpace(configurationFile.AppSettings.IpWhiteList))
            if (ConfigurationValueParser.TryParseIpRanges(configurationFile.AppSettings.IpWhiteList,
                    out var ipWhiteList))
            {
                dto.IpWhiteList = ipWhiteList;
            }
            else
            {
                dto.IpWhiteList = [];
                var exception = InvalidConfigurationException.For(c => c.AppSettings.IpWhiteList,
                    formatedMessage, configurationFile.AppSettings.IpWhiteList);
                StartupLogger.Warning(exception.Message);
            }

        if (!string.IsNullOrWhiteSpace(configurationFile.AppSettings.InvalidCredentialDelay))
            if (ConfigurationValueParser.TryParseDelaySettings(configurationFile.AppSettings.InvalidCredentialDelay,
                    out var tuple))
            {
                dto.InvalidCredentialDelay = tuple;
            }
            else
            {
                var exception = InvalidConfigurationException.For(c => c.AppSettings.InvalidCredentialDelay,
                    formatedMessage, configurationFile.AppSettings.InvalidCredentialDelay);
                StartupLogger.Warning(exception.Message);
            }

        var firstFactorAuthenticationSource =
            !string.IsNullOrWhiteSpace(configurationFile.AppSettings.FirstFactorAuthenticationSource)
                ? configurationFile.AppSettings.FirstFactorAuthenticationSource
                : throw InvalidConfigurationException.For(prop => prop.AppSettings.FirstFactorAuthenticationSource,
                    "Property '{prop}' is required. Config name: '{0}'", configurationFile.FileName);

        dto.FirstFactorAuthenticationSource =
            ConfigurationValueParser.TryParseEnum<AuthenticationSource>(firstFactorAuthenticationSource, out var source)
                ? source
                : throw InvalidConfigurationException.For(c => c.AppSettings.FirstFactorAuthenticationSource,
                    "Error while cast property '{prop}'. Value: {0}. Config name: '{1}'",
                    firstFactorAuthenticationSource, configurationFile.FileName);

        var adapterClientEndpoint =
            dto.FirstFactorAuthenticationSource != AuthenticationSource.Radius ||
            !string.IsNullOrWhiteSpace(configurationFile.AppSettings.AdapterClientEndpoint)
                ? configurationFile.AppSettings.AdapterClientEndpoint
                : throw InvalidConfigurationException.For(c => c.AppSettings.AdapterClientEndpoint,
                    "Property '{prop}' is required. Config name: '{0}'", configurationFile.FileName);
        if (!string.IsNullOrWhiteSpace(configurationFile.AppSettings.AdapterClientEndpoint))
            dto.AdapterClientEndpoint =
                ConfigurationValueParser.TryParseEndpoint(adapterClientEndpoint, out var endpoint)
                    ? endpoint!
                    : throw InvalidConfigurationException.For(c => c.AppSettings.AdapterClientEndpoint,
                        "Error while cast property '{prop}'. Value: {0}. Config name: '{1}'", adapterClientEndpoint,
                        configurationFile.FileName);
        ;
        return dto;
    }
}