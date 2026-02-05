using Multifactor.Radius.Adapter.v2.Application.Configuration.Models;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Configurations.Loader;

public interface IConfigurationLoader
{
    ServiceConfiguration Load(); 
}