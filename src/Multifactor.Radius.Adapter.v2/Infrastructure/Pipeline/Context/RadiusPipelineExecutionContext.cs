using Multifactor.Radius.Adapter.v2.Core.Auth;
using Multifactor.Radius.Adapter.v2.Core.Ldap;
using Multifactor.Radius.Adapter.v2.Core.Pipeline;
using Multifactor.Radius.Adapter.v2.Core.Radius.Packet;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline.Context;

public class RadiusPipelineExecutionContext : IRadiusPipelineExecutionContext
{
    public IPipelineExecutionSettings Settings { get; }
    public ILdapProfile UserLdapProfile { get; }
    public IRadiusPacket RequestPacket { get; }
    public IRadiusPacket ResponsePacket { get; }
    public IAuthenticationState AuthenticationState { get; }
    public IResponseInformation ResponseInformation { get; }
    public IExecutionState ExecutionState { get; }
}