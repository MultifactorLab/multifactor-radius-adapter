using Microsoft.Extensions.Configuration;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Configurations.Reader;

public static class ConfigurationBuilderExtensions
{
    public static IConfigurationBuilder AddLegacyXmlConfig(
        this IConfigurationBuilder builder, 
        string path)
    {
        return builder.Add(new XmlConfigurationSource(path));
    }
    
    public static IConfigurationBuilder AddPrefixEnvironmentVariables(
        this IConfigurationBuilder builder,
        string prefix)
    {
        return builder.Add(new PrefixEnvironmentVariablesConfigurationSource(prefix));
    }
}