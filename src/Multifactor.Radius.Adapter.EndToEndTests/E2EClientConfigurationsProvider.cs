using Multifactor.Radius.Adapter.EndToEndTests.Fixtures.Models;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration.Models;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration.XmlAppConfiguration;

namespace Multifactor.Radius.Adapter.EndToEndTests;

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