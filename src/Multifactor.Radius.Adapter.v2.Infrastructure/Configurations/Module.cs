using Microsoft.Extensions.DependencyInjection;
using Multifactor.Radius.Adapter.v2.Application.Core.Models;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configurations.Loader;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configurations.Models.Dictionary;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Configurations;

public static class Module
{
    public static IServiceCollection AddConfiguration(this IServiceCollection services)
    {        
        services.AddTransient<RadiusDictionary>();
        services.AddSingleton<IRadiusDictionary>(prov =>
        {
            var dict = prov.GetRequiredService<RadiusDictionary>();
            dict.Read();
            return dict;
        });
        services.AddSingleton<IConfigurationLoader, ConfigurationLoader>();

        services.AddSingleton<ServiceConfiguration>(provider =>
        {
            var manager = provider.GetRequiredService<IConfigurationLoader>();
            return manager.Load();
        });
        return services;
    }
}