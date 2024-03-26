using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MultiFactor.Radius.Adapter;
using Serilog;
using Serilog.Events;
using System;
using System.Text;

IHost host = null;
try
{
    var builder = Host.CreateApplicationBuilder(args);
    builder.ConfigureApplication();

    host = builder.Build();
    host.Run();
}
catch (Exception ex)
{
    var errorMessage = FlattenException(ex);

    if (Log.Logger != null && Log.IsEnabled(LogEventLevel.Error))
    {
        Log.Logger.Error($"Unable to start: {errorMessage}");
    }
    else
    {
        Console.WriteLine($"Unable to start: {errorMessage}");
    }

    if (host != null)
    {
        await host.StopAsync();
    }
}

static string FlattenException(Exception exception)
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
