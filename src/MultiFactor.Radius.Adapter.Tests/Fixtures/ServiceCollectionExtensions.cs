using Microsoft.Extensions.DependencyInjection;

namespace MultiFactor.Radius.Adapter.Tests.Fixtures;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection RemoveService<TService>(this IServiceCollection services) where TService : class
    {
        var descr = services.FirstOrDefault(x => x.ServiceType == typeof(TService));
        if (descr == null) return services;
        services.Remove(descr);
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
