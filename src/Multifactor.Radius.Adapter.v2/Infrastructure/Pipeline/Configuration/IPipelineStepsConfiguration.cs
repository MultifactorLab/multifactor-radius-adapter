using Multifactor.Radius.Adapter.v2.Core.Auth.PreAuthMode;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline;

public interface IPipelineStepsConfiguration
{
    public string ConfigurationName { get; }
    public PreAuthMode PreAuthMode { get; }
}