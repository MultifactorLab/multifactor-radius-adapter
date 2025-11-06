using Multifactor.Radius.Adapter.v2.Core.Configuration.Service;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configuration.RadiusAdapter;

namespace Multifactor.Radius.Adapter.v2.Core.Configuration.Client.Build;

public interface IClientConfigurationFactory
{
    IClientConfiguration CreateConfig(string name, RadiusAdapterConfiguration configuration, IServiceConfiguration serviceConfig);
}