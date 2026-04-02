using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Multifactor.Radius.Adapter.v2.Application.Core.Models;

namespace Multifactor.Radius.Adapter.v2.Application.Extensions_remove_;

public static class ApplicationExtensions
{
    public static void AddApplicationVariables(this IServiceCollection services)
    {
        var appVars = new ApplicationVariables //todo
        {
            AppVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString(),
            StartedAt = DateTime.Now
        };
        services.AddSingleton(appVars);
    }
}