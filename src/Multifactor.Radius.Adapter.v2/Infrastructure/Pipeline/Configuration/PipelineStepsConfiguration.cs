using Multifactor.Radius.Adapter.v2.Domain.Auth;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline.Configuration;

public class PipelineStepsConfiguration : IPipelineStepsConfiguration
{
    public string ConfigurationName { get; }

    public PreAuthMode PreAuthMode { get; }
    
    public bool ShouldLoadUserGroups { get; }
    
    // TODO Maybe use something better
    public bool HasLdapServers { get; }

    public PipelineStepsConfiguration(string configurationName, PreAuthMode preAuthMode, bool shouldLoadGroups = false, bool hasLdapServers = false)
    {
        if (string.IsNullOrWhiteSpace(configurationName))
            throw new ArgumentException($"'{nameof(configurationName)}' cannot be null or whitespace.", nameof(configurationName));
    
        ConfigurationName = configurationName;
        PreAuthMode = preAuthMode;
        ShouldLoadUserGroups = shouldLoadGroups;
        HasLdapServers = hasLdapServers;
    }
}