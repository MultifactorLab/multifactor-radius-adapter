using Multifactor.Radius.Adapter.v2.Core.Auth.PreAuthMode;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline;

public class PipelineStepsConfiguration : IPipelineStepsConfiguration
{
    public string ConfigurationName { get; }

    public PreAuthMode PreAuthMode { get; }

    public bool ShouldLoadUserGroups { get; }

    public PipelineStepsConfiguration(string configurationName, PreAuthMode preAuthMode, bool shouldLoadGroups = false)
    {
        if (string.IsNullOrWhiteSpace(configurationName))
        {
            throw new ArgumentException($"'{nameof(configurationName)}' cannot be null or whitespace.", nameof(configurationName));
        }
        
        ConfigurationName = configurationName;
        PreAuthMode = preAuthMode;
        ShouldLoadUserGroups = shouldLoadGroups;
    }
}