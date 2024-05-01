using MultiFactor.Radius.Adapter.Configuration.ConfigurationLoading;

namespace MultiFactor.Radius.Adapter.Tests.AdapterConfig;

internal class TestableAppConfigConfigurationSource : AppConfigConfigurationSource
{
    public TestableAppConfigConfigurationSource(RadiusConfigurationFile path) : base(path)
    {
    }

    public IDictionary<string, string?> AllData => Data;
}
