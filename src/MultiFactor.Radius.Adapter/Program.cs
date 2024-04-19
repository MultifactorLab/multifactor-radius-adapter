using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MultiFactor.Radius.Adapter.Extensions;
using MultiFactor.Radius.Adapter.Framework;
using MultiFactor.Radius.Adapter.Server.Pipeline.AccessChallenge;
using MultiFactor.Radius.Adapter.Server.Pipeline.AccessRequestFilter;
using MultiFactor.Radius.Adapter.Server.Pipeline.FirstFactorAuthentication;
using MultiFactor.Radius.Adapter.Server.Pipeline.PreSecondFactorAuthentication;
using MultiFactor.Radius.Adapter.Server.Pipeline.SecondFactorAuthentication;
using MultiFactor.Radius.Adapter.Server.Pipeline.StatusServer;
using MultiFactor.Radius.Adapter.Server.Pipeline.TransformUserName;
using Serilog;
using Serilog.Events;
using System;
using System.Text;


IHost host = null;

try
{
    var builder = RadiusHost.CreateApplicationBuilder(args);
    builder.AddLogging();
    builder.ConfigureApplication();

    builder.UseMiddleware<StatusServerMiddleware>();
    builder.UseMiddleware<AccessRequestFilterMiddleware>();
    builder.UseMiddleware<TransformUserNameMiddleware>();
    builder.UseMiddleware<AccessChallengeMiddleware>();
    builder.UseMiddleware<AnonymousFirstFactorAuthenticationMiddleware>();
    builder.UseMiddleware<PreSecondFactorAuthenticationMiddleware>();
    builder.UseMiddleware<FirstFactorAuthenticationMiddleware>();
    builder.UseMiddleware<SecondFactorAuthenticationMiddleware>();

    host = builder.Build();
    host.Run();
}
catch (Exception ex)
{
    var errorMessage = FlattenException(ex);

    if (Log.Logger != null && Log.IsEnabled(LogEventLevel.Error))
    {
        Log.Logger.Error(ex, "Unable to start: {Message:l}", errorMessage);
    }
    else
    {
        Console.WriteLine($"Unable to start: {errorMessage}");
    }

    await host?.StopAsync();
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
