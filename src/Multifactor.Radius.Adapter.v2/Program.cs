using System.Reflection;
using System.Text;
using Multifactor.Radius.Adapter.v2.Core;
using Multifactor.Radius.Adapter.v2.Extensions;
using Multifactor.Radius.Adapter.v2.Server;
using Multifactor.Radius.Adapter.v2.Server.Udp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Multifactor.Radius.Adapter.v2.Core.Pipeline;
using Multifactor.Radius.Adapter.v2.Infrastructure.Logging;
using Multifactor.Radius.Adapter.v2.Services;

IHost? host = null;
try
{
    var builder = Host.CreateApplicationBuilder(args);
    builder.Services.AddMemoryCache();
    builder.AddLogging();
    
    var appVars = new ApplicationVariables
    {
        AppPath = Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory),
        AppVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString(),
        StartedAt = DateTime.Now
    };
    
    builder.Services.AddSingleton(appVars);
    builder.Services.AddRadiusDictionary();
    builder.Services.AddConfiguration();
    
    builder.Services.AddLdapSchemaLoader();
    builder.Services.AddDataProtection();
    
    builder.Services.AddFirstFactor();
    builder.Services.AddPipelines();
   
    builder.Services.AddSingleton<IUdpPacketHandler, UdpPacketHandler>();
    builder.Services.AddTransient<IResponseSender, AdapterResponseSender>();
    
    builder.Services.AddServices();
    builder.Services.AddChallenge();
    
    builder.Services.AddUdpClient();
    builder.Services.AddMultifactorHttpClient();
    
    builder.Services.AddSingleton<AdapterServer>();
    builder.Services.AddHostedService<ServerHost>();
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