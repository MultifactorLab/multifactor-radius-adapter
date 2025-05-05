using Multifactor.Radius.Adapter.v2.Core.Auth;
using Multifactor.Radius.Adapter.v2.Core.Ldap;
using Multifactor.Radius.Adapter.v2.Core.Pipeline;
using Multifactor.Radius.Adapter.v2.Core.Radius;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline.Context;

public interface IRadiusPipelineExecutionContext
{
    IPipelineExecutionSettings Settings { get; }
    ILdapProfile UserLdapProfile { get; }
    IRadiusPacket RequestPacket { get; }
    IRadiusPacket ResponsePacket { get; }
    IAuthenticationState AuthenticationState { get;  }
}