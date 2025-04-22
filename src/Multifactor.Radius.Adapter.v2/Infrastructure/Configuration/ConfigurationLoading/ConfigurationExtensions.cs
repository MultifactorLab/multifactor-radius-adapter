//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using Microsoft.Extensions.Configuration;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Configuration.ConfigurationLoading;

public static class ConfigurationExtensions
{
    public static RadiusAdapterConfiguration BindRadiusAdapterConfig(this IConfiguration configuration)
    {
        if (configuration is null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        return configuration.Get<RadiusAdapterConfiguration>(x =>
        {
            x.BindNonPublicProperties = true;
            x.ErrorOnUnknownConfiguration = false;
        });
    }
}
