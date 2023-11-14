using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace MultiFactor.Radius.Adapter.Tests.Fixtures;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection RemoveService<TService>(this IServiceCollection services) where TService : class
    {
        services.RemoveAll<TService>();
        return services;
    }

    public static IServiceCollection ReplaceSingletoneImpl<TService, TImplementation>(this IServiceCollection services, TImplementation implementation)
         where TService : class where TImplementation : class, TService
    {
        var oldImpl = services.FirstOrDefault(x => x.ServiceType == typeof(TService));
        if (oldImpl != null)
        {
            services.Remove(oldImpl);
        }

        return services.AddSingleton<TService>(implementation);
    }

    public static IServiceCollection ReplaceSingletoneImpl<TService, TImplementation>(this IServiceCollection services)
         where TService : class where TImplementation : class, TService
    {
        var oldImpl = services.FirstOrDefault(x => x.ServiceType == typeof(TService));
        if (oldImpl != null)
        {
            services.Remove(oldImpl);
        }
        return services.AddSingleton<TService, TImplementation>();
    }
}
