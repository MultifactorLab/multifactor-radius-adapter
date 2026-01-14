using System.Net;
using Multifactor.Radius.Adapter.v2.Application.Models.Enum;
using NetTools;

namespace Multifactor.Radius.Adapter.v2.Application.Configuration.Models;

public class ClientConfiguration
{
    public required string Name { get; init; }
    
    public string MultifactorNasIdentifier { get; set; } = string.Empty;
    public string MultifactorSharedSecret { get; set; } = string.Empty;
    public IReadOnlyList<string> SignUpGroups { get; set; } = [];
    public bool BypassSecondFactorWhenApiUnreachable { get; set; } = true;
    public AuthenticationSource FirstFactorAuthenticationSource { get; set; }
    public required IPEndPoint AdapterClientEndpoint { get; set; }
    
    public IPAddress? RadiusClientIp { get; set; }
    public string RadiusClientNasIdentifier { get; set; } = string.Empty;
    public string RadiusSharedSecret { get; set; } = string.Empty;
    public IPEndPoint[] NpsServerEndpoints { get; set; }
    public TimeSpan NpsServerTimeout { get; set; } //"00:00:05"
    
    public PrivacyMode PrivacyMode { get; set; }
    public string[] PrivacyFields { get; set; } = [];
    public PreAuthMode? PreAuthenticationMethod { get; set; }
    public TimeSpan AuthenticationCacheLifetime { get; set; } = TimeSpan.Zero;
    public (int min, int max) InvalidCredentialDelay { get; set; }
    public string CallingStationIdAttribute { get; set; } = string.Empty;
    public IReadOnlyList<IPAddressRange> IpWhiteList { get; set; }
    public string LoggingLevel { get; set; } = string.Empty;
    
    public IReadOnlyList<LdapServerConfiguration>? LdapServers { get; set; }
    
    public IReadOnlyDictionary<string, RadiusReplyAttribute[]> ReplyAttributes { get; set; }
}