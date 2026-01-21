using System.Net;
using Multifactor.Radius.Adapter.v2.Application.Configuration.Models.Enum;
using Multifactor.Radius.Adapter.v2.Shared.Attributes;
using NetTools;

namespace Multifactor.Radius.Adapter.v2.Application.Configuration.Models;

public class ClientConfiguration
{
    public string Name { get; set; }
    
    [ConfigParameter("multifactor-nas-identifier")]
    public string MultifactorNasIdentifier { get; set; }
    [ConfigParameter("multifactor-shared-secret")]
    public string MultifactorSharedSecret { get; set; }
    [ConfigParameter("sign-up-group")]
    public IReadOnlyList<string> SignUpGroups { get; set; } = [];
    [ConfigParameter("bypass-second-factor-when-api-unreachable",true)]
    public bool BypassSecondFactorWhenApiUnreachable { get; set; }
    [ConfigParameter("first-factor-authentication-source")]
    public AuthenticationSource FirstFactorAuthenticationSource { get; set; }
    [ConfigParameter("adapter-client-endpoint")]
    public IPEndPoint AdapterClientEndpoint { get; set; }
    
    [ConfigParameter("radius-client-ip")]
    public IPAddress? RadiusClientIp { get; set; }
    [ConfigParameter("radius-client-nas-identifier")]
    public string RadiusClientNasIdentifier { get; set; }
    [ConfigParameter("radius-shared-secret")]
    public string RadiusSharedSecret { get; set; }
    [ConfigParameter("nps-server-endpoint")]
    public IPEndPoint[] NpsServerEndpoints { get; set; }
    [ConfigParameter("nps-server-timeout")]
    public TimeSpan NpsServerTimeout { get; set; } //"00:00:05"
    
    [ConfigParameter("privacy-mode")]
    public (PrivacyMode PrivacyMode, string[] PrivacyFields) Privacy { get; set; }

    [ConfigParameter("pre-authentication-method")]
    public PreAuthMode? PreAuthenticationMethod { get; set; }
    [ConfigParameter("authentication-cache-lifetime")]
    public TimeSpan AuthenticationCacheLifetime { get; set; } = TimeSpan.Zero;
    [ConfigParameter("invalid-credential-delay")]
    public (int min, int max) InvalidCredentialDelay { get; set; }
    [ConfigParameter("calling-station-id-attribute")]
    public string CallingStationIdAttribute { get; set; }
    [ConfigParameter("ip-white-list")]
    public IReadOnlyList<IPAddressRange> IpWhiteList { get; set; }
    
    [ComplexConfigParameter("ldapServers")]
    public IReadOnlyList<LdapServerConfiguration>? LdapServers { get; set; }
    [ComplexConfigParameter("RadiusReply")]
    public IReadOnlyDictionary<string, RadiusReplyAttribute[]>? ReplyAttributes { get; set; }
}