using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MultiFactor.Radius.Adapter;
using Serilog;
using System;
using System.Text;

var builder = Host.CreateApplicationBuilder(args);
builder.ConfigureApplication();

var host = builder.Build();

try
{
    host.Run();
}
catch (Exception ex)
{
    var errorMessage = FlattenException(ex);

    if (Log.Logger != null)
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
