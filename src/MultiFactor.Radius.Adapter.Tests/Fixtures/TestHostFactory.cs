using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace MultiFactor.Radius.Adapter.Tests.Fixtures
{
    internal static class TestHostFactory
    {
        public static IHost CreateHost(Action<IServiceCollection>? configureServices = null)
        {
            var builder = Host.CreateApplicationBuilder();
            builder.Services.ConfigureServices(configureServices);
            return builder.Build();
        }
    }
}