using Multifactor.Radius.Adapter.v2.Infrastructure.Configuration.RadiusAdapter;

namespace Multifactor.Radius.Adapter.v2.Core.Configuration.Service.Build;

public interface IServiceConfigurationFactory
{
    IServiceConfiguration CreateConfig(RadiusAdapterConfiguration rootConfiguration);
}