using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Multifactor.Radius.Adapter.v2.Application.Extensions_remove_;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configurations;
using Multifactor.Radius.Adapter.v2.Infrastructure.Extensions_remove_;
using Multifactor.Radius.Adapter.v2.Infrastructure.Integrations;
using Multifactor.Radius.Adapter.v2.Infrastructure.Logging;
using Multifactor.Radius.Adapter.v2.Server;

IHost? host = null;
try
{
    var builder = Host.CreateApplicationBuilder(args);
    builder.Services.AddWindowsService(options => options.ServiceName = "Multifactor RADIUS");
    builder.Services.AddMemoryCache();
    builder.Services.AddApplicationVariables();
    builder.Services.AddConfiguration();
    builder.Services.AddAdapterLogging();
    builder.Services.AddIntegrations();    
    builder.Services.AddResponseSender();
    builder.Services.AddInfraServices();
    builder.Services.AddRadiusUdpClient();
    builder.Services.AddServer();
    
    host = builder.Build();
    host.Run();
}
catch (Exception ex)
{
    var errorMessage = FlattenException(ex);
    StartupLogger.Error(ex, "Unable to start: {Message:l}", errorMessage);
}
finally
{
    await (host?.StopAsync() ?? Task.CompletedTask);
}
return;

static string FlattenException(Exception? exception)
{
    var stringBuilder = new StringBuilder();
    var counter = 0;
    while (exception != null)
    {
        if (counter++ > 0)
        {
            var prefix = new string('-', counter) + ">\t";
            stringBuilder.Append(prefix);
        }
        stringBuilder.AppendLine(exception.Message);
        exception = exception.InnerException;
    }
    return stringBuilder.ToString();
}