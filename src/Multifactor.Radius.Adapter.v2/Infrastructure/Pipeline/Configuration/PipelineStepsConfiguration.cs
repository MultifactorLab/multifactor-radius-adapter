using Multifactor.Radius.Adapter.v2.Infrastructure.Features;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline;

public class PipelineStepsConfiguration : IPipelineStepsConfiguration
{
    private readonly string _configurationName;
    private readonly PreAuthMode _preAuthMode;
    private readonly bool _shouldCheckMembership;

    public string ConfigurationName => _configurationName;
    public PreAuthMode PreAuthMode => _preAuthMode;
    public bool ShouldCheckMembership => _shouldCheckMembership;
    
    public PipelineStepsConfiguration(string configurationName, PreAuthMode preAuthMode, bool shouldCheckMembership)
    {
        if (string.IsNullOrWhiteSpace(configurationName))
        {
            throw new ArgumentException($"'{nameof(configurationName)}' cannot be null or whitespace.", nameof(configurationName));
        }
        
        _configurationName = configurationName;
        _preAuthMode = preAuthMode;
        _shouldCheckMembership = shouldCheckMembership;
    }
}