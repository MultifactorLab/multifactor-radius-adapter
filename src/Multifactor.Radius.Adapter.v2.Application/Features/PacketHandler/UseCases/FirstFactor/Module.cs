using Microsoft.Extensions.DependencyInjection;
using Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.FirstFactor.Processor;

namespace Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.FirstFactor;

public static class Module
{
    public static IServiceCollection AddFirstFactor(this IServiceCollection services)
    {
        services.AddSingleton<IFirstFactorProcessorProvider, FirstFactorProcessorProvider>();
        services.AddTransient<IFirstFactorProcessor, LdapFirstFactorProcessor>();
        services.AddTransient<IFirstFactorProcessor, RadiusFirstFactorProcessor>();
        services.AddTransient<IFirstFactorProcessor, NoneFirstFactorProcessor>();
        services.AddTransient<FirstFactorStep>();
        
        return services;
    }
}