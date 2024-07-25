//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using Microsoft.Extensions.Configuration;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration.Models;
using System;

namespace MultiFactor.Radius.Adapter.Infrastructure.Configuration.ConfigurationLoading;

internal static class ConfigurationExtensions
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
