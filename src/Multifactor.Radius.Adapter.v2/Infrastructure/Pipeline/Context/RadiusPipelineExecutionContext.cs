using Multifactor.Radius.Adapter.v2.Core.Radius;
using Multifactor.Radius.Adapter.v2.Ldap;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline.Context;

public class RadiusPipelineExecutionContext : IRadiusPipelineExecutionContext
{
    public IRadiusSettings Settings { get; }
    public ILdapProfile UserLdapProfile { get; }
    public IRadiusPacket Packet { get; }
}