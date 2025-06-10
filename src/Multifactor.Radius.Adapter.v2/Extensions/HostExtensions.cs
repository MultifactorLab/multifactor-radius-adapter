using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configuration.RadiusAdapter.Build;
using Multifactor.Radius.Adapter.v2.Infrastructure.Logging;
using Serilog;

namespace Multifactor.Radius.Adapter.v2.Extensions;

public static class HostExtensions
{
    public static void AddLogging(this HostApplicationBuilder builder)
    {
        var rootConfig = RadiusAdapterConfigurationProvider.GetRootConfiguration();
        var logger = SerilogLoggerFactory.CreateLogger(rootConfig);
        Log.Logger = logger;

        builder.Logging.ClearProviders();
        builder.Logging.AddSerilog(logger);
    }
}