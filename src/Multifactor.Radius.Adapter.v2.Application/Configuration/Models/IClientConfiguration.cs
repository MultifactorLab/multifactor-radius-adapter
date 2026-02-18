using System.Net;
using Multifactor.Radius.Adapter.v2.Application.Core.Models;
using Multifactor.Radius.Adapter.v2.Application.Core.Models.Enum;
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
    
    public IReadOnlyList<IPAddress?> RadiusClientIps { get; }
    public string RadiusClientNasIdentifier { get; }
    public string RadiusSharedSecret { get; }
    public IReadOnlyList<IPEndPoint> NpsServerEndpoints { get; }
    public TimeSpan NpsServerTimeout { get; }
    
    public Privacy Privacy { get; }

    public PreAuthMode? PreAuthenticationMethod { get; }
    public TimeSpan AuthenticationCacheLifetime { get; }
    public CredentialDelay? InvalidCredentialDelay { get; }
    public string? CallingStationIdAttribute { get; }  
    public IReadOnlyList<IPAddressRange> IpWhiteList { get; }
    
    public IReadOnlyList<ILdapServerConfiguration>? LdapServers { get; }
    public IReadOnlyDictionary<string, IReadOnlyList<IRadiusReplyAttribute>>? ReplyAttributes { get; }
}

