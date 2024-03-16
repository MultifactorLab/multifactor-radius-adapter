using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using MultiFactor.Radius.Adapter.Core.Pipeline;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace MultiFactor.Radius.Adapter.Core;

/// <summary>
/// Represents a hosted RADIUS applications and services builder. [Wraps the default <see cref="HostApplicationBuilder"/>].
/// </summary>
internal class RadiusHostApplicationBuilder
{
    private readonly List<Func<RadiusRequestDelegate, RadiusRequestDelegate>> _components = new();

    /// <summary>
    /// Returns original <see cref="HostApplicationBuilder"/> that has been wrapped by the current RadiusHostApplicationBuilder.
    /// </summary>
    public HostApplicationBuilder InternalHostApplicationBuilder { get; }
    public IServiceCollection Services => InternalHostApplicationBuilder.Services;

    public RadiusHostApplicationBuilder(HostApplicationBuilder hostApplicationBuilder)
    {
        InternalHostApplicationBuilder = hostApplicationBuilder ?? throw new ArgumentNullException(nameof(hostApplicationBuilder));
    }

    /// <summary>
    /// Adds a middleware to the radius request pipeline.
    /// </summary>
    /// <typeparam name="TMiddleware">Middleware type.</typeparam>
    /// <returns><see cref="RadiusHostApplicationBuilder"/></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public RadiusHostApplicationBuilder UseMiddleware<TMiddleware>() where TMiddleware : IRadiusMiddleware
    {
        Services.AddTransient(typeof(TMiddleware));
        Func<RadiusRequestDelegate, RadiusRequestDelegate> middleware = next =>
        {
            return async context =>
            {
                TMiddleware middleware;

                try
                {
                    middleware = context.RequestServices.GetRequiredService<TMiddleware>();
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Unable to create middleware {typeof(TMiddleware)}: {ex.Message}", ex);
                }

                await middleware.InvokeAsync(context, next);
            };
        };

        _components.Add(middleware);
        return this;
    }

    /// <summary>
    /// Builds and validate <see cref="IHost"/> and returns it from the internal host builder.
    /// </summary>
    /// <returns></returns>
    public IHost Build()
    {
        var requestDelegate = BuildRequestDelegate();
        Services.AddSingleton(requestDelegate);
        Services.AddSingleton<IRadiusPipeline, RadiusPipeline>();

        return InternalHostApplicationBuilder.Build();
    }

    private RadiusRequestDelegate BuildRequestDelegate()
    {
        RadiusRequestDelegate pipeline = context => Task.CompletedTask;
        for (var c = _components.Count - 1; c >= 0; c--)
        {
            pipeline = _components[c](pipeline);
        }
        return pipeline;
    }
}
