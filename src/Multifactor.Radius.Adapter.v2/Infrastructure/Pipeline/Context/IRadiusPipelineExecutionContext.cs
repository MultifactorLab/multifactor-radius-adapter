using Multifactor.Radius.Adapter.v2.Core;
using Multifactor.Radius.Adapter.v2.Core.Radius;
using Multifactor.Radius.Adapter.v2.Ldap;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline.Context;

public interface IRadiusPipelineExecutionContext
{
    IRadiusSettings Settings { get; }
    ILdapProfile UserLdapProfile { get; }
    IRadiusPacket RequestPacket { get; }
    IRadiusPacket ResponsePacket { get; }
    IAuthenticationState AuthenticationState { get;  }
}