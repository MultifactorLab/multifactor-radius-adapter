using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Multifactor.Radius.Adapter.v2.Application.Core.Models;
using Multifactor.Radius.Adapter.v2.Application.SharedPorts;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.ShaderAdapters;

public static class Module
{
    public static IServiceCollection AddSharedPorts(this IServiceCollection services)
    {
        services.AddTransient<IChallengeContextCache, ChallengeContextCache>();
        services.AddTransient<ICheckMembership, CheckMembership>();
        services.AddTransient<ILoadLdapSchema, LoadLdapSchema>();
        services.AddUdpClient();
        return services;
    }
    
    private static IServiceCollection AddUdpClient(this IServiceCollection services)
    {        
        return services.AddSingleton<IUdpClient>(serviceProvider =>
        {
            var config = serviceProvider.GetRequiredService<ServiceConfiguration>();
            var endpoint = config.RootConfiguration.AdapterServerEndpoint;
            var logger = serviceProvider.GetService<ILogger<CustomUdpClient>>();
            return new CustomUdpClient(endpoint, logger!);
        });
    }
}