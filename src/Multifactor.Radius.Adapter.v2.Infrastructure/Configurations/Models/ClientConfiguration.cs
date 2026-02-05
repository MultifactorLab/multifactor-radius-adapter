using System.Net;
using Multifactor.Radius.Adapter.v2.Application.Configuration.Models;
using Multifactor.Radius.Adapter.v2.Application.Configuration.Models.Enum;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configurations.Exceptions;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configurations.Parser;
using NetTools;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Configurations.Models;

public class ClientConfiguration : IClientConfiguration
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
    public IPEndPoint[] NpsServerEndpoints { get; set; }
    public TimeSpan NpsServerTimeout { get; set; }
    
    public (PrivacyMode PrivacyMode, string[] PrivacyFields) Privacy { get; set; }

    public PreAuthMode? PreAuthenticationMethod { get; set; }
    public TimeSpan AuthenticationCacheLifetime { get; set; } = TimeSpan.Zero;
    public (int min, int max)? InvalidCredentialDelay { get; set; }
    public string? CallingStationIdAttribute { get; set; } //TODO not used
    public IReadOnlyList<IPAddressRange> IpWhiteList { get; set; }
    
    public IReadOnlyList<ILdapServerConfiguration>? LdapServers { get; set; }
    public IReadOnlyDictionary<string, IRadiusReplyAttribute[]>? ReplyAttributes { get; set; }


    public static ClientConfiguration FromConfiguration(ConfigurationFile configurationFile)
    {
        ArgumentNullException.ThrowIfNull(configurationFile);
        var dto = new ClientConfiguration
        {
            Name = configurationFile.FileName,
            MultifactorNasIdentifier = !string.IsNullOrWhiteSpace(configurationFile.AppSettings.MultifactorNasIdentifier) ? configurationFile.AppSettings.MultifactorNasIdentifier 
                : throw InvalidConfigurationException.RequiredFor(c => c.AppSettings.MultifactorNasIdentifier, configurationFile.FileName),
            MultifactorSharedSecret = !string.IsNullOrWhiteSpace(configurationFile.AppSettings.MultifactorSharedSecret) ? configurationFile.AppSettings.MultifactorNasIdentifier 
                : throw InvalidConfigurationException.RequiredFor(c => c.AppSettings.MultifactorSharedSecret, configurationFile.FileName),
            SignUpGroups = ConfigurationValueProcessor.TryParseStringList(configurationFile.AppSettings.SignUpGroups, out var list) ? list : [],
            BypassSecondFactorWhenApiUnreachable = configurationFile.AppSettings.BypassSecondFactorWhenApiUnreachable,
            RadiusClientIp = ConfigurationValueProcessor.TryParseIpAddress(configurationFile.AppSettings.RadiusClientIp, out var address) ? address : null,
            RadiusClientNasIdentifier = configurationFile.AppSettings.RadiusClientNasIdentifier,
            RadiusSharedSecret = !string.IsNullOrWhiteSpace(configurationFile.AppSettings.RadiusSharedSecret) ? configurationFile.AppSettings.RadiusSharedSecret 
                : throw InvalidConfigurationException.For(c => c.AppSettings.RadiusSharedSecret, "Property '{prop}' is required. Config name: '{1}'", configurationFile.FileName),
            NpsServerEndpoints = ConfigurationValueProcessor.TryParseEndpoints(configurationFile.AppSettings.NpsServerEndpoints, out var npsServerEndpoints)
                ? npsServerEndpoints : [],
            NpsServerTimeout = ConfigurationValueProcessor.TryParseTimeout(configurationFile.AppSettings.NpsServerTimeout, out var timeout) ? timeout.Value : TimeSpan.Parse("00:00:05"),
            Privacy = ConfigurationValueProcessor.TryParsePrivacyModeWithFields(configurationFile.AppSettings.Privacy, out var privacy) ?  privacy : new(PrivacyMode.None, []),
            PreAuthenticationMethod = ConfigurationValueProcessor.TryParseEnum<PreAuthMode>(configurationFile.AppSettings.PreAuthenticationMethod, out var mode) ? mode : PreAuthMode.None,
            AuthenticationCacheLifetime = ConfigurationValueProcessor.TryParseTimeSpan(configurationFile.AppSettings.AuthenticationCacheLifetime, out var span) ? span : TimeSpan.Zero,
            CallingStationIdAttribute = configurationFile.AppSettings.CallingStationIdAttribute,
            IpWhiteList = ConfigurationValueProcessor.TryParseIpRanges(configurationFile.AppSettings.IpWhiteList, out var ipWhiteList) ? ipWhiteList : [],
            InvalidCredentialDelay = ConfigurationValueProcessor.TryParseDelaySettings(configurationFile.AppSettings.InvalidCredentialDelay, out var tuple)
                ? tuple
                : null
        };

        var firstFactorAuthenticationSource = !string.IsNullOrWhiteSpace(configurationFile.AppSettings.FirstFactorAuthenticationSource) ? configurationFile.AppSettings.FirstFactorAuthenticationSource :
            throw InvalidConfigurationException.RequiredFor(c => c.AppSettings.FirstFactorAuthenticationSource, configurationFile.FileName);
        dto.FirstFactorAuthenticationSource = ConfigurationValueProcessor.TryParseEnum<AuthenticationSource>(firstFactorAuthenticationSource, out var source)
            ? source 
            : throw InvalidConfigurationException.For(c => c.AppSettings.FirstFactorAuthenticationSource, "Error while cast property '{prop}'. Config name: '{1}'", configurationFile.FileName); ;
        
        var adapterClientEndpoint = !string.IsNullOrWhiteSpace(configurationFile.AppSettings.AdapterClientEndpoint) ? configurationFile.AppSettings.AdapterClientEndpoint :
            throw InvalidConfigurationException.For(c => c.AppSettings.AdapterClientEndpoint, "Property '{prop}' is required. Config name: '{1}'", configurationFile.FileName);
        dto.AdapterClientEndpoint =
            ConfigurationValueProcessor.TryParseEndpoint(adapterClientEndpoint, out var endpoint)
                ? endpoint! :  
                throw InvalidConfigurationException.For(c => c.AppSettings.FirstFactorAuthenticationSource, "Error while cast property '{prop}'. Config name: '{1}'", configurationFile.FileName); ;
        return dto;
    }
}