using Microsoft.Extensions.DependencyInjection;
using Multifactor.Radius.Adapter.v2.Application.Core.Models;
using Serilog;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Logging;

public static class Module
{
    public static IServiceCollection AddAdapterLogging(this IServiceCollection services)
    {
        return services.AddSerilog((provider, loggerConfiguration) =>
        {
            var serviceConfiguration = provider.GetRequiredService<ServiceConfiguration>();
            SerilogLoggerFactory.CreateLogger(loggerConfiguration, serviceConfiguration.RootConfiguration);
        });
    }
}