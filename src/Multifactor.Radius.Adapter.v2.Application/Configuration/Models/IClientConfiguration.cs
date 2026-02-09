using System.Net;
using Multifactor.Radius.Adapter.v2.Application.Configuration.Models.Enum;
using NetTools;

namespace Multifactor.Radius.Adapter.v2.Application.Configuration.Models;

public interface IClientConfiguration
{
    public string Name { get; }
    
    public string MultifactorNasIdentifier { get; }
    public string MultifactorSharedSecret { get; }
    public IReadOnlyList<string> SignUpGroups { get; }
    public bool BypassSecondFactorWhenApiUnreachable { get; }
    public AuthenticationSource FirstFactorAuthenticationSource { get; }
    public IPEndPoint AdapterClientEndpoint { get; }
    
    public IPAddress? RadiusClientIp { get; }
    public string RadiusClientNasIdentifier { get; }
    public string RadiusSharedSecret { get; }
    public IReadOnlyList<IPEndPoint> NpsServerEndpoints { get; }
    public TimeSpan NpsServerTimeout { get; }
    
    public Privacy Privacy { get; }

    public PreAuthMode? PreAuthenticationMethod { get; }
    public TimeSpan AuthenticationCacheLifetime { get; }
    public CredentialDelay? InvalidCredentialDelay { get; }
    public string? CallingStationIdAttribute { get; } //TODO not used
    public IReadOnlyList<IPAddressRange> IpWhiteList { get; }
    
    public IReadOnlyList<ILdapServerConfiguration>? LdapServers { get; }
    public IReadOnlyDictionary<string, IReadOnlyList<IRadiusReplyAttribute>>? ReplyAttributes { get; }
}

public record Privacy(PrivacyMode PrivacyMode, string[] PrivacyFields);

public record CredentialDelay(int Min, int Max);