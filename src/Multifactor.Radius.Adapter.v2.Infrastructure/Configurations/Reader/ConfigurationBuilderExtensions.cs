using Microsoft.Extensions.Configuration;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Configurations.Reader;

internal static class ConfigurationBuilderExtensions
{
    public static IConfigurationBuilder AddXmlConfig(
        this IConfigurationBuilder builder, 
        string path)
    {
        return builder.Add(new XmlConfigurationSource(path));
    }
    
    public static IConfigurationBuilder AddEnvironmentVariables(
        this IConfigurationBuilder builder,
        string prefix)
    {
        return builder.Add(new PrefixEnvironmentVariablesConfigurationSource(prefix));
    }
}