﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MultiFactor.Radius.Adapter.Core.Framework.Pipeline;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MultiFactor.Radius.Adapter.Core.Framework.Exceptions;

namespace MultiFactor.Radius.Adapter.Core.Framework;

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

    /// <summary>
    /// Gets a collection of services for the radius application to compose. This is useful for adding user provided or framework provided services.
    /// </summary>
    public IServiceCollection Services => InternalHostApplicationBuilder.Services;

    public RadiusHostApplicationBuilder(HostApplicationBuilder hostApplicationBuilder)
    {
        InternalHostApplicationBuilder = hostApplicationBuilder ?? throw new ArgumentNullException(nameof(hostApplicationBuilder));
    }

    /// <summary>
    /// Adds a middleware type to the radius application's request pipeline.
    /// </summary>
    /// <typeparam name="TMiddleware">The middleware type.</typeparam>
    /// <returns>The <see cref="RadiusHostApplicationBuilder"/> instance.</returns>
    /// <exception cref="InvalidOperationException"></exception>
    public RadiusHostApplicationBuilder UseMiddleware<TMiddleware>() where TMiddleware : IRadiusMiddleware
    {
        Services.AddTransient(typeof(TMiddleware));
        RadiusRequestDelegate middleware(RadiusRequestDelegate next)
        {
            return context =>
            {
                if (context.Flags.TerminateFlag)
                {
                    return Task.CompletedTask;
                }

                TMiddleware mw;

                try
                {
                    mw = context.RequestServices.GetRequiredService<TMiddleware>();
                }
                catch (Exception ex)
                {
                    throw new RadiusPipelineException($"Unable to create middleware {typeof(TMiddleware)}: {ex.Message}", ex);
                }

                return mw.InvokeAsync(context, next);
            };
        }

        _components.Add(middleware);
        return this;
    }

    /// <summary>
    /// Builds <see cref="IHost"/> and returns it from the internal host builder.
    /// </summary>
    /// <returns></returns>
    public IHost Build()
    {
        var requestDelegate = BuildRequestDelegate();
        Services.AddSingleton(requestDelegate);
        Services.AddSingleton<RadiusPipeline>();

        return InternalHostApplicationBuilder.Build();
    }

    /// <summary>
    /// Composes a chain of the delegates.<br/>
    /// From the list [delegate1, delegate2, delegate3] you will get the following call chain:
    /// <code>
    /// delegate1(RadiusContext context) 
    /// {
    ///     delegate2(RadiusContext context)
    ///     {
    ///         delegate3(RadiusContext context) {}
    ///     }
    /// }
    /// </code>
    /// </summary>
    /// <returns></returns>
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
