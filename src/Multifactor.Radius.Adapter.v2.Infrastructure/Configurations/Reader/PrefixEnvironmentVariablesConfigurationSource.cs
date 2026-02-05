using Microsoft.Extensions.Configuration;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Configurations.Reader;

public class PrefixEnvironmentVariablesConfigurationSource : IConfigurationSource
{
    private readonly string _prefix;
    
    public PrefixEnvironmentVariablesConfigurationSource(string prefix)
    {
        _prefix = prefix;
    }
    
    public IConfigurationProvider Build(IConfigurationBuilder builder)
        => new PrefixEnvironmentVariablesConfigurationProvider(_prefix);
}