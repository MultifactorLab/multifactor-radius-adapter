using Multifactor.Radius.Adapter.v2.Core.Auth.PreAuthMode;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline;

public class PipelineStepsConfiguration : IPipelineStepsConfiguration
{
    private readonly string _configurationName;
    private readonly PreAuthMode _preAuthMode;

    public string ConfigurationName => _configurationName;
    public PreAuthMode PreAuthMode => _preAuthMode;
    
    public PipelineStepsConfiguration(string configurationName, PreAuthMode preAuthMode)
    {
        if (string.IsNullOrWhiteSpace(configurationName))
        {
            throw new ArgumentException($"'{nameof(configurationName)}' cannot be null or whitespace.", nameof(configurationName));
        }
        
        _configurationName = configurationName;
        _preAuthMode = preAuthMode;
    }
}