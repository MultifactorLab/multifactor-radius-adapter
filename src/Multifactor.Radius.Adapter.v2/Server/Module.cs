using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Multifactor.Radius.Adapter.v2.Application.Core.Models;
using Multifactor.Radius.Adapter.v2.Features.PacketHandle;

namespace Multifactor.Radius.Adapter.v2.Server;

public static class Module
{
    public static IServiceCollection AddServer(this IServiceCollection services)
    {
        var appVars = new ApplicationVariables
        {
            AppVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString(),
            StartedAt = DateTime.Now
        };
        services.AddSingleton(appVars);
        services.AddPacketHandleFeature();
        services.AddSingleton<AdapterServer>();
        services.AddHostedService<ServerHost>();
        return services;
    }
}