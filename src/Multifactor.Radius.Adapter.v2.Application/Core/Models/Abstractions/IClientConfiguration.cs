using System.Net;
using Multifactor.Radius.Adapter.v2.Application.Core.Enum;

namespace Multifactor.Radius.Adapter.v2.Application.Core.Models.Abstractions;

public interface IClientConfiguration
{
    public string Name { get; }
    
    public string MultifactorNasIdentifier { get; }
    public string MultifactorSharedSecret { get; }
    public bool BypassSecondFactorWhenApiUnreachable { get; }
    public AuthenticationSource FirstFactorAuthenticationSource { get; }
    public IPEndPoint AdapterClientEndpoint { get; }
    public IReadOnlyList<string> SignUpGroups { get; }
    
    public string RadiusClientNasIdentifier { get; }
    public string RadiusSharedSecret { get; }
    public TimeSpan NpsServerTimeout { get; }
    public IReadOnlyList<IpEntry>? RadiusClientIps { get; }
    public IReadOnlyList<IpEntry>? RadiusClientNasIps { get; }
    public IReadOnlyList<IPEndPoint> NpsServerEndpoints { get; }
    
    public Privacy Privacy { get; }

    public PreAuthMode? PreAuthenticationMethod { get; }
    public TimeSpan AuthenticationCacheLifetime { get; }
    public CredentialDelay? InvalidCredentialDelay { get; }
    public string? CallingStationIdAttribute { get; }  
    public bool IsIpFromUdp { get; }  
    public IReadOnlyList<IpEntry> IpWhiteList { get; }
    public bool IsAccessChallengePassword { get; }

    
    public IReadOnlyList<ILdapServerConfiguration>? LdapServers { get; }
    public IReadOnlyDictionary<string, IReadOnlyList<IRadiusReplyAttribute>>? ReplyAttributes { get; }
}

