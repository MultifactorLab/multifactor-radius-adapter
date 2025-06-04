using System.Net;
using Multifactor.Core.Ldap.Schema;
using Multifactor.Radius.Adapter.v2.Core.Auth;
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
    public IRadiusPacket ResponsePacket { get; set; }
    public IAuthenticationState AuthenticationState { get; }
    public IResponseInformation ResponseInformation { get; }
    public IExecutionState ExecutionState { get; }
    public string MustChangePasswordDomain { get; set; }
    public IPEndPoint RemoteEndpoint { get; set; }
    public IPEndPoint ProxyEndpoint { get; set; }
    public ILdapSchema? LdapSchema { get; set; }
}