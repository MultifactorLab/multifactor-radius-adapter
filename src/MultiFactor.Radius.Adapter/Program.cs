using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MultiFactor.Radius.Adapter;
using MultiFactor.Radius.Adapter.Core;
using MultiFactor.Radius.Adapter.Server.Pipeline;
using Serilog;
using System;
using System.Text;

var builder = RadiusHost.CreateApplicationBuilder(args);
builder.Configure(x => x.ConfigureApplication());

builder.UseMiddleware<StatusServerMiddleware>();
builder.UseMiddleware<AccessRequestFilterMiddleware>();
builder.UseMiddleware<TransformUserNameMiddleware>();
builder.UseMiddleware<AccessChallengeMiddleware>();
builder.UseMiddleware<FirstFactorAuthenticationMiddleware>();
builder.UseMiddleware<Bypass2FaMidleware>();
builder.UseMiddleware<SecondFactorAuthenticationMiddleware>();

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
