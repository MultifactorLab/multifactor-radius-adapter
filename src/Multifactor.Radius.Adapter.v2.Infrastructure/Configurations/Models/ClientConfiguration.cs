using System.Net;
using Multifactor.Radius.Adapter.v2.Application.Configuration.Models;
using Multifactor.Radius.Adapter.v2.Application.Configuration.Models.Enum;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configurations.Exceptions;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configurations.Parser;
using NetTools;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Configurations.Models;

internal class ClientConfiguration : IClientConfiguration
{
    public string Name { get; set; }
    
    public string MultifactorNasIdentifier { get; set; }
    public string MultifactorSharedSecret { get; set; }
    public IReadOnlyList<string> SignUpGroups { get; set; } = [];
    public bool BypassSecondFactorWhenApiUnreachable { get; set; }
    public AuthenticationSource FirstFactorAuthenticationSource { get; set; }
    public IPEndPoint AdapterClientEndpoint { get; set; }
    
    public IPAddress? RadiusClientIp { get; set; }
    public string RadiusClientNasIdentifier { get; set; }
    public string RadiusSharedSecret { get; set; }
    public IReadOnlyList<IPEndPoint> NpsServerEndpoints { get; set; }
    public TimeSpan NpsServerTimeout { get; set; }
    
    public Privacy Privacy { get; set; }

    public PreAuthMode? PreAuthenticationMethod { get; set; }
    public TimeSpan AuthenticationCacheLifetime { get; set; } = TimeSpan.Zero;
    public CredentialDelay? InvalidCredentialDelay { get; set; }
    public string? CallingStationIdAttribute { get; set; } //TODO not used
    public IReadOnlyList<IPAddressRange> IpWhiteList { get; set; }
    
    public IReadOnlyList<ILdapServerConfiguration>? LdapServers { get; set; }
    public IReadOnlyDictionary<string, IReadOnlyList<IRadiusReplyAttribute>>? ReplyAttributes { get; set; }


    public static ClientConfiguration FromConfiguration(AdapterConfiguration configurationFile)
    {
        ArgumentNullException.ThrowIfNull(configurationFile);
        var dto = new ClientConfiguration
        {
            Name = configurationFile.FileName,
            MultifactorNasIdentifier = !string.IsNullOrWhiteSpace(configurationFile.AppSettings.MultifactorNasIdentifier) ? configurationFile.AppSettings.MultifactorNasIdentifier :
                throw InvalidConfigurationException.For(prop => prop.AppSettings.MultifactorNasIdentifier, "Property '{prop}' is required. Config name: '{0}'",  configurationFile.FileName),
            MultifactorSharedSecret = !string.IsNullOrWhiteSpace(configurationFile.AppSettings.MultifactorSharedSecret) ? configurationFile.AppSettings.MultifactorSharedSecret :
                throw InvalidConfigurationException.For(prop => prop.AppSettings.MultifactorSharedSecret, "Property '{prop}' is required. Config name: '{0}'",  configurationFile.FileName),
            SignUpGroups = ConfigurationValueParser.TryParseStringList(configurationFile.AppSettings.SignUpGroups, out var list) ? list : [],
            BypassSecondFactorWhenApiUnreachable = configurationFile.AppSettings.BypassSecondFactorWhenApiUnreachable,
            RadiusClientIp = ConfigurationValueParser.TryParseIpAddress(configurationFile.AppSettings.RadiusClientIp, out var address) ? address : null,
            RadiusClientNasIdentifier = configurationFile.AppSettings.RadiusClientNasIdentifier,
            RadiusSharedSecret = !string.IsNullOrWhiteSpace(configurationFile.AppSettings.RadiusSharedSecret) ? configurationFile.AppSettings.RadiusSharedSecret 
                : throw InvalidConfigurationException.For(c => c.AppSettings.RadiusSharedSecret, "Property '{prop}' is required. Config name: '{0}'", configurationFile.FileName),
            NpsServerEndpoints = ConfigurationValueParser.TryParseEndpoints(configurationFile.AppSettings.NpsServerEndpoints, out var npsServerEndpoints)
                ? npsServerEndpoints : [],
            NpsServerTimeout = ConfigurationValueParser.TryParseTimeout(configurationFile.AppSettings.NpsServerTimeout, out var timeout) ? timeout.Value : TimeSpan.Parse("00:00:05"),
            Privacy = ConfigurationValueParser.TryParsePrivacyModeWithFields(configurationFile.AppSettings.Privacy, out var privacy) ?  privacy : new(PrivacyMode.None, []),
            PreAuthenticationMethod = ConfigurationValueParser.TryParseEnum<PreAuthMode>(configurationFile.AppSettings.PreAuthenticationMethod, out var mode) ? mode : PreAuthMode.None,
            AuthenticationCacheLifetime = ConfigurationValueParser.TryParseTimeSpan(configurationFile.AppSettings.AuthenticationCacheLifetime, out var span) ? span : TimeSpan.Zero,
            CallingStationIdAttribute = configurationFile.AppSettings.CallingStationIdAttribute,
            IpWhiteList = ConfigurationValueParser.TryParseIpRanges(configurationFile.AppSettings.IpWhiteList, out var ipWhiteList) ? ipWhiteList : [],
            InvalidCredentialDelay = ConfigurationValueParser.TryParseDelaySettings(configurationFile.AppSettings.InvalidCredentialDelay, out var tuple)
                ? tuple
                : null
        };

        var firstFactorAuthenticationSource =
            !string.IsNullOrWhiteSpace(configurationFile.AppSettings.FirstFactorAuthenticationSource)
                ? configurationFile.AppSettings.FirstFactorAuthenticationSource
                : throw InvalidConfigurationException.For(prop => prop.AppSettings.FirstFactorAuthenticationSource,
                    "Property '{prop}' is required. Config name: '{0}'", configurationFile.FileName);

        dto.FirstFactorAuthenticationSource = ConfigurationValueParser.TryParseEnum<AuthenticationSource>(firstFactorAuthenticationSource, out var source)
            ? source 
            : throw InvalidConfigurationException.For(c => c.AppSettings.FirstFactorAuthenticationSource, "Error while cast property '{prop}'. Value: {0}. Config name: '{1}'", firstFactorAuthenticationSource, configurationFile.FileName); ;
        
        var adapterClientEndpoint = dto.FirstFactorAuthenticationSource != AuthenticationSource.Radius || !string.IsNullOrWhiteSpace(configurationFile.AppSettings.AdapterClientEndpoint) ? configurationFile.AppSettings.AdapterClientEndpoint :
            throw InvalidConfigurationException.For(c => c.AppSettings.AdapterClientEndpoint, "Property '{prop}' is required. Config name: '{0}'", configurationFile.FileName);
        if(!string.IsNullOrWhiteSpace(configurationFile.AppSettings.AdapterClientEndpoint))
            dto.AdapterClientEndpoint =
            ConfigurationValueParser.TryParseEndpoint(adapterClientEndpoint, out var endpoint)
                ? endpoint! :  
                throw InvalidConfigurationException.For(c => c.AppSettings.AdapterClientEndpoint, "Error while cast property '{prop}'. Value: {0}. Config name: '{1}'", adapterClientEndpoint, configurationFile.FileName); ;
        return dto;
    }
}