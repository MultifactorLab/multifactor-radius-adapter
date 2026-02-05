using System.Net;
using Multifactor.Radius.Adapter.v2.Application.Configuration.Models.Enum;
using NetTools;

namespace Multifactor.Radius.Adapter.v2.Application.Configuration.Models;

public interface IClientConfiguration
{
    public string Name { get; set; }
    
    public string MultifactorNasIdentifier { get; set; }
    public string MultifactorSharedSecret { get; set; }
    public IReadOnlyList<string> SignUpGroups { get; set; }
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
    public TimeSpan AuthenticationCacheLifetime { get; set; }
    public (int min, int max)? InvalidCredentialDelay { get; set; }
    public string? CallingStationIdAttribute { get; set; } //TODO not used
    public IReadOnlyList<IPAddressRange> IpWhiteList { get; set; }
    
    public IReadOnlyList<ILdapServerConfiguration>? LdapServers { get; set; }
    public IReadOnlyDictionary<string, IRadiusReplyAttribute[]>? ReplyAttributes { get; set; }
}