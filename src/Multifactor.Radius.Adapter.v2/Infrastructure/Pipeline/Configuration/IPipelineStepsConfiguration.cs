using Multifactor.Radius.Adapter.v2.Domain.Auth;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline.Configuration;

public interface IPipelineStepsConfiguration
{
    public string ConfigurationName { get; }
    public PreAuthMode PreAuthMode { get; }
    bool ShouldLoadUserGroups { get; }
    public bool HasLdapServers { get; }
}