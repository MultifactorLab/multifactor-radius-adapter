using Multifactor.Radius.Adapter.v2.Infrastructure.Configuration.RadiusAdapter;

namespace Multifactor.Radius.Adapter.v2.EndToEndTests.Fixtures.Models;

public class E2ERadiusConfiguration(
    RadiusAdapterConfiguration rootConfig,
    Dictionary<string, RadiusAdapterConfiguration>? clientConfigs = null)
{
    public RadiusAdapterConfiguration RootConfiguration { get; } = rootConfig;
    public Dictionary<string, RadiusAdapterConfiguration>? ClientConfigs { get; } = clientConfigs;
}