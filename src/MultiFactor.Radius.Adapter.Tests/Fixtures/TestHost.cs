using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MultiFactor.Radius.Adapter.Core.Framework.Context;
using MultiFactor.Radius.Adapter.Core.Framework.Pipeline;
using MultiFactor.Radius.Adapter.Core.Radius;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration;
using MultiFactor.Radius.Adapter.Server;
using System.Net;

namespace MultiFactor.Radius.Adapter.Tests.Fixtures;

internal class TestHost
{
    private readonly IHost _host;

    public TestHost(IHost host)
    {
        _host = host ?? throw new ArgumentNullException(nameof(host));
    }

    public TService Service<TService>() where TService : class => _host.Services.GetRequiredService<TService>();

    /// <summary>
    /// Creates Radius Context which can be passed to the Radius Pipeline.<br/> 
    /// Local radius endpoint will be setted as 127.0.0.1:1812.
    /// </summary>
    /// <param name="requestPacket">Radius request packet.</param>
    /// <param name="clientConfig">If defined, the client config will be specified. Othervise the client config will be getted from the first element of <see cref="IServiceConfiguration.Clients"/>.</param>
    /// <param name="setupContext">Setup context action.</param>
    /// <returns><see cref="RadiusContext"/></returns>
    public RadiusContext CreateContext(IRadiusPacket requestPacket, 
        IClientConfiguration? clientConfig = null, 
        Action<RadiusContext>? setupContext = null)
    {
        if (requestPacket is null)
        {
            throw new ArgumentNullException(nameof(requestPacket));
        }

        var factory = Service<RadiusContextFactory>();
        var localEndpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 1812);
        var remote = new IPEndPoint(IPAddress.Loopback, 1812);
        var config = clientConfig ?? Service<IServiceConfiguration>().Clients[0];

        var ctx = factory.CreateContext(config, requestPacket, remote, null);
        setupContext?.Invoke(ctx);

        return ctx;
    }

    public Task InvokePipeline(RadiusContext ctx)
    {
        var pipeline = Service<RadiusPipeline>();
        return pipeline.InvokeAsync(ctx);
    }
}