using Multifactor.Radius.Adapter.v2.Domain.RadiusAdapter;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configuration.Service;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Configuration.Client.Build;

public interface IClientConfigurationFactory
{
    IClientConfiguration CreateConfig(string name, RadiusAdapterConfiguration configuration, IServiceConfiguration serviceConfig);
}