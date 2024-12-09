using Microsoft.Extensions.Hosting;
using MultiFactor.Radius.Adapter.Core.Framework;
using MultiFactor.Radius.Adapter.Extensions;
using MultiFactor.Radius.Adapter.Server.Pipeline.AccessChallenge;
using MultiFactor.Radius.Adapter.Server.Pipeline.AccessRequestFilter;
using MultiFactor.Radius.Adapter.Server.Pipeline.FirstFactorAuthentication;
using MultiFactor.Radius.Adapter.Server.Pipeline.PreSecondFactorAuthentication;
using MultiFactor.Radius.Adapter.Server.Pipeline.SecondFactorAuthentication;
using MultiFactor.Radius.Adapter.Server.Pipeline.StatusServer;
using System;
using System.Text;
using System.Threading.Tasks;
using MultiFactor.Radius.Adapter.Infrastructure.Logging;

IHost host = null;

try
{
    var builder = RadiusHost.CreateApplicationBuilder(args);
    builder.AddLogging();
    builder.ConfigureApplication();

    builder.UseMiddleware<StatusServerMiddleware>();
    builder.UseMiddleware<AccessRequestFilterMiddleware>();
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
    StartupLogger.Error(ex, "Unable to start: {Message:l}", errorMessage);
}
finally
{
    await (host?.StopAsync() ?? Task.CompletedTask);
}

return;

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

public partial class RdsEntryPoint { }