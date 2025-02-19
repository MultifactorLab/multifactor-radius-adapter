using MultiFactor.Radius.Adapter.Infrastructure.Configuration.Models;

namespace Multifactor.Radius.Adapter.EndToEndTests.Fixtures.Models;

public class E2ERadiusConfiguration(
    RadiusAdapterConfiguration rootConfig,
    Dictionary<string, RadiusAdapterConfiguration>? clientConfigs = null)
{
    public RadiusAdapterConfiguration RootConfiguration { get; } = rootConfig;
    public Dictionary<string, RadiusAdapterConfiguration>? ClientConfigs { get; } = clientConfigs;
}