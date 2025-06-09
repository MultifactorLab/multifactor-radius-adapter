using System.Net;
using Multifactor.Core.Ldap.LangFeatures;
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
    public ILdapProfile UserLdapProfile { get; }
    public IRadiusPacket RequestPacket { get; }
    public IRadiusPacket? ResponsePacket { get; set; }
    public IAuthenticationState AuthenticationState { get; } = new AuthenticationState();
    public IResponseInformation ResponseInformation { get; } = new ResponseInformation();
    public IExecutionState ExecutionState { get; } = new ExecutionState();
    public string MustChangePasswordDomain { get; set; }
    public IPEndPoint RemoteEndpoint { get; set; }
    public IPEndPoint? ProxyEndpoint { get; set; }
    
    public ILdapServerConfiguration FirstFactorLdapServerConfiguration { get; set; }

    public RadiusPipelineExecutionContext(IPipelineExecutionSettings settings, IRadiusPacket requestPacket)
    {
        Throw.IfNull(settings, nameof(settings));
        Throw.IfNull(requestPacket, nameof(requestPacket));
        
        Settings = settings;
        RequestPacket = requestPacket;
    }
}