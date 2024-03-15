using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using MultiFactor.Radius.Adapter.Core.Pipeline;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace MultiFactor.Radius.Adapter.Core;

internal class RadiusHostApplicationBuilder
{
    private readonly List<Func<RadiusRequestDelegate, RadiusRequestDelegate>> _components = new();
    private readonly HostApplicationBuilder _hostApplicationBuilder;

    public RadiusHostApplicationBuilder(HostApplicationBuilder hostApplicationBuilder)
    {
        _hostApplicationBuilder = hostApplicationBuilder ?? throw new ArgumentNullException(nameof(hostApplicationBuilder));
    }

    public RadiusHostApplicationBuilder UseMiddleware<TMiddleware>() where TMiddleware : IRadiusMiddleware
    {
        _hostApplicationBuilder.Services.AddTransient(typeof(TMiddleware));
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

    public RadiusHostApplicationBuilder Configure(Action<HostApplicationBuilder> action)
    {
        if (action is null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        action(_hostApplicationBuilder);
        return this;
    }

    public IHost Build()
    {
        var requestDelegate = BuildRequestDelegate();
        _hostApplicationBuilder.Services.AddSingleton(requestDelegate);
        _hostApplicationBuilder.Services.AddSingleton<IRadiusPipeline, RadiusPipeline>();

        return _hostApplicationBuilder.Build();
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
