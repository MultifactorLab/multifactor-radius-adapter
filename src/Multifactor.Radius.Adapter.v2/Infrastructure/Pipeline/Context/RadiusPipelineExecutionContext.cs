using System.Net;
using Multifactor.Core.Ldap.LangFeatures;
using Multifactor.Core.Ldap.Schema;
using Multifactor.Radius.Adapter.v2.Core;
using Multifactor.Radius.Adapter.v2.Core.Auth;
using Multifactor.Radius.Adapter.v2.Core.Configuration.Client;
using Multifactor.Radius.Adapter.v2.Core.Ldap;
using Multifactor.Radius.Adapter.v2.Core.Pipeline;
using Multifactor.Radius.Adapter.v2.Core.Pipeline.Settings;
using Multifactor.Radius.Adapter.v2.Core.Radius.Packet;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline.Context;

public class RadiusPipelineExecutionContext : IRadiusPipelineExecutionContext
{
    public IPipelineExecutionSettings Settings { get; }
    public ILdapProfile UserLdapProfile { get; set; }
    public IRadiusPacket RequestPacket { get; }
    public IRadiusPacket? ResponsePacket { get; set; }
    public IExecutionState ExecutionState { get; } = new ExecutionState();
    public IAuthenticationState AuthenticationState { get; set; } = new AuthenticationState();
    public IResponseInformation ResponseInformation { get; set; } = new ResponseInformation();
    public string MustChangePasswordDomain { get; set; }
    public IPEndPoint RemoteEndpoint { get; set; }
    public IPEndPoint? ProxyEndpoint { get; set; }
    public ILdapSchema? LdapSchema { get; set; }
    public string State { get; set; }
    public UserPassphrase Passphrase { get; set; }
    public ILdapServerConfiguration FirstFactorLdapServerConfiguration { get; set; }

    public RadiusPipelineExecutionContext(IPipelineExecutionSettings settings, IRadiusPacket requestPacket)
    {
        Throw.IfNull(settings, nameof(settings));
        Throw.IfNull(requestPacket, nameof(requestPacket));
        
        Settings = settings;
        RequestPacket = requestPacket;
    }

}