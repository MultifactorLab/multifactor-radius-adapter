using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using MultiFactor.Radius.Adapter.Configuration.ConfigurationLoading;
using MultiFactor.Radius.Adapter.Configuration.Core;
using MultiFactor.Radius.Adapter.Core;
using Serilog;
using System.Linq;
using SerilogLoggerFactory = MultiFactor.Radius.Adapter.Logging.SerilogLoggerFactory;

namespace MultiFactor.Radius.Adapter;

internal static class AddLoggingExtension
{
    public static RadiusHostApplicationBuilder AddLogging(this RadiusHostApplicationBuilder builder)
    {
        // temporary service provider.
        var services = new ServiceCollection();

        var appVarDescriptor = builder.InternalHostApplicationBuilder.Services.FirstOrDefault(x => x.ServiceType == typeof(ApplicationVariables));
        if (appVarDescriptor == null)
        {
            throw new System.Exception($"Service type '{typeof(ApplicationVariables)}' was not found in the RadiusHostApplicationBuilder services");
        }
        services.Add(appVarDescriptor);

        var rootConfigProvDescriptor = builder.InternalHostApplicationBuilder.Services.FirstOrDefault(x => x.ServiceType == typeof(IRootConfigurationProvider));
        if (rootConfigProvDescriptor == null)
        {
            throw new System.Exception($"Service type '{typeof(IRootConfigurationProvider)}' was not found in the RadiusHostApplicationBuilder services");
        }
        services.Add(rootConfigProvDescriptor);

        services.AddSingleton<SerilogLoggerFactory>();

        var prov = services.BuildServiceProvider();
        var logger = prov.GetRequiredService<SerilogLoggerFactory>().CreateLogger();

        Log.Logger = logger;
        builder.InternalHostApplicationBuilder.Logging.ClearProviders();
        builder.InternalHostApplicationBuilder.Logging.AddSerilog(logger);

        return builder;
    }
}
