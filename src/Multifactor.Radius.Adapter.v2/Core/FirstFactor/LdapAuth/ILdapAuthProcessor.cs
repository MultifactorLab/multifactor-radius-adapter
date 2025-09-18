using Multifactor.Radius.Adapter.v2.Core.Radius;
using Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline.Context;

namespace Multifactor.Radius.Adapter.v2.Core.FirstFactor.LdapAuth;

public interface ILdapAuthProcessor
{
    Task<AuthResult> Auth( IRadiusPipelineExecutionContext context);
    AuthenticationType AuthenticationType { get; }
}