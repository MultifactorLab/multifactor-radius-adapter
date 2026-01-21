using Multifactor.Core.Ldap.Schema;
using Multifactor.Radius.Adapter.v2.Application.Configuration.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.Ldap.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.Models.Enum;
using Multifactor.Radius.Adapter.v2.Application.Features.Radius.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.Radius.Models.Enums;

namespace Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.Models;

public class RadiusPipelineContext
{
    public RadiusPacket RequestPacket { get; }
    public ClientConfiguration ClientConfiguration { get; }
    public LdapServerConfiguration? LdapConfiguration { get; }
    public UserPassphrase? Passphrase { get; set; }
    public ILdapSchema? LdapSchema { get; set; }
    public ILdapProfile? LdapProfile { get; set; }
    public string MustChangePasswordDomain  { get; set; }
    public HashSet<string> UserGroups { get; set; } = [];

    public RadiusPacket? ResponsePacket { get; set; }
    public ResponseInformation ResponseInformation { get; set; } = new();
    public AuthenticationStatus FirstFactorStatus { get; set; }
    public AuthenticationStatus SecondFactorStatus { get; set; }
    
    public bool IsTerminated { get; private set; }
    public bool ShouldSkipResponse { get; private set; }
    public bool IsDomainAccount => RequestPacket.AccountType == AccountType.Domain;
    public void Terminate() => IsTerminated = true;
    public void SkipResponse() => ShouldSkipResponse = true;

    public RadiusPipelineContext(
        RadiusPacket requestPacket,
        ClientConfiguration clientConfiguration,
        LdapServerConfiguration? ldapServerConfig = null)
    {
        RequestPacket = requestPacket;
        ClientConfiguration = clientConfiguration;
        LdapConfiguration = ldapServerConfig;
    }
}
