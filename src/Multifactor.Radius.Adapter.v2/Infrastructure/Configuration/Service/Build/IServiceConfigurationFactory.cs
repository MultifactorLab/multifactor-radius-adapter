using Multifactor.Radius.Adapter.v2.Domain.RadiusAdapter;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Configuration.Service.Build;

public interface IServiceConfigurationFactory
{
    IServiceConfiguration CreateConfig(RadiusAdapterConfiguration rootConfiguration);
}