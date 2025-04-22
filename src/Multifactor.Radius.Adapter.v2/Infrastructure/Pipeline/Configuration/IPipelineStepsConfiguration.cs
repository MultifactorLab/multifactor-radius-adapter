using Multifactor.Radius.Adapter.v2.Infrastructure.Features;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline;

public interface IPipelineStepsConfiguration
{
    public string ConfigurationName { get; }
    public PreAuthMode PreAuthMode { get; }
    public bool ShouldCheckMembership { get; } 
}