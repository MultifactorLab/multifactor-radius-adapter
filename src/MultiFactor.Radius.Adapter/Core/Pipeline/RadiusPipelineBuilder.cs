using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using Microsoft.AspNetCore.Http;
using static System.Net.Mime.MediaTypeNames;

namespace MultiFactor.Radius.Adapter.Core.Pipeline;

public class RadiusPipelineBuilder : IRadiusPipelineBuilder
{
    private readonly List<Func<RadiusRequestDelegate, RadiusRequestDelegate>> _components = new();
    private readonly IServiceCollection _services;

    public RadiusPipelineBuilder(IServiceCollection services)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
    }

    public IRadiusPipelineBuilder Use<TMiddleware>() where TMiddleware : IRadiusMiddleware
    {
        _services.AddTransient(typeof(TMiddleware));
        Func<RadiusRequestDelegate, RadiusRequestDelegate> middleware = next =>
        {
            return async context =>
            {
                var middleware = context.RequestServices.GetService<TMiddleware>();
                if (middleware == null)
                {
                    throw new InvalidOperationException($"Unable to create middleware {typeof(TMiddleware)}");
                }

                await middleware.InvokeAsync(context, next);
            };
        };

        _components.Add(middleware);
        return this;
    }

    private RadiusRequestDelegate Execute<TMiddleware>(RadiusRequestDelegate next) where TMiddleware : IRadiusMiddleware
    {
        return async context =>
        {
            var middleware = context.RequestServices.GetService<TMiddleware>();
            if (middleware == null)
            {
                throw new InvalidOperationException($"Unable to create middleware {typeof(TMiddleware)}");
            }

            await middleware.InvokeAsync(context, next);
        };
    }

    public RadiusRequestDelegate BuildPipelineDelegate()
    {
        RadiusRequestDelegate pipeline = context => Task.CompletedTask;
        for (var c = _components.Count - 1; c >= 0; c--)
        {
            pipeline = _components[c](pipeline);
        }
        return pipeline;
    }
}
