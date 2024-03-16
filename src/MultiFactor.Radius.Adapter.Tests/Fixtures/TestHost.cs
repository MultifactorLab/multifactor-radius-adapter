using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MultiFactor.Radius.Adapter.Configuration.Core;
using MultiFactor.Radius.Adapter.Core.Radius;
using MultiFactor.Radius.Adapter.Server.Context;
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

    public RadiusContext CreateContext(IRadiusPacket packet)
    {
        var factory = Service<RadiusContextFactory>();
        var config = Service<IServiceConfiguration>();
        var createUdpClient = Service<Func<IPEndPoint, IUdpClient>>();
        var localEndpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 1812);
        var remote = new IPEndPoint(IPAddress.Loopback, 1812);
        return factory.CreateContext(config.Clients[0], packet, createUdpClient(localEndpoint), remote, null);
    }
}