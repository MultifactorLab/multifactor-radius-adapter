using Multifactor.Radius.Adapter.v2.Core.Configuration.Client.Build;
using Multifactor.Radius.Adapter.v2.EndToEndTests.Fixtures.Models;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configuration.RadiusAdapter;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configuration.XmlAppConfiguration;

namespace Multifactor.Radius.Adapter.v2.EndToEndTests;

public class E2EClientConfigurationsProvider : IClientConfigurationsProvider
{
    private readonly Dictionary<string, RadiusAdapterConfiguration> _clientConfigurations;
    
    public E2EClientConfigurationsProvider(Dictionary<string, RadiusAdapterConfiguration>? clientConfigurations)
    {
        _clientConfigurations = clientConfigurations ?? new Dictionary<string, RadiusAdapterConfiguration>();
    }

    public RadiusConfigurationSource GetSource(RadiusAdapterConfiguration configuration)
    {
        return new RadiusConfigurationModel(_clientConfigurations.FirstOrDefault(x => x.Value == configuration).Key);
    }

    public RadiusAdapterConfiguration[] GetClientConfigurations()
    {
        return _clientConfigurations.Select(x => x.Value).ToArray();
    }
}