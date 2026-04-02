using Multifactor.Core.Ldap.Schema;
using Multifactor.Radius.Adapter.v2.Application.Core.Enum;
using Multifactor.Radius.Adapter.v2.Application.Core.Models.Abstractions;
using Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.LoadLdapForest.Models;

namespace Multifactor.Radius.Adapter.v2.Application.Core.Models;

public sealed class RadiusPipelineContext
{
    public RadiusPacket RequestPacket { get; }
    public IClientConfiguration ClientConfiguration { get; }
    public ILdapServerConfiguration? LdapConfiguration { get; }
    public UserPassphrase? Passphrase { get; set; }
    public ILdapSchema? LdapSchema { get; set; }
    public ILdapProfile? LdapProfile { get; set; }
    public IForestMetadata? ForestMetadata { get; set; }
    public string MustChangePasswordDomain  { get; set; }
    public HashSet<string> UserGroups { get; set; } = [];

    public RadiusPacket? ResponsePacket { get; set; }
    public AuthenticationStatus FirstFactorStatus { get; set; }
    public AuthenticationStatus SecondFactorStatus { get; set; }
    
    public bool IsTerminated { get; private set; }
    public bool ShouldSkipResponse { get; private set; }
    public bool IsDomainAccount => RequestPacket.AccountType == AccountType.Domain;
    public ResponseInformation ResponseInformation { get; set; } = new();
    public void Terminate() => IsTerminated = true;
    public void SkipResponse() => ShouldSkipResponse = true;

    public RadiusPipelineContext(
        RadiusPacket requestPacket,
        IClientConfiguration clientConfiguration,
        ILdapServerConfiguration? ldapServerConfig = null)
    {
        RequestPacket = requestPacket;
        ClientConfiguration = clientConfiguration;
        LdapConfiguration = ldapServerConfig;
    }
}
