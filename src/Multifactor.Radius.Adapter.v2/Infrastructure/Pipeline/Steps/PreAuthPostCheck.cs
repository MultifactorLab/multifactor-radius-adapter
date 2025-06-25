using Microsoft.Extensions.Logging;
using Multifactor.Radius.Adapter.v2.Core.Auth;
using Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline.Context;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline.Steps;

public class PreAuthPostCheck : IRadiusPipelineStep
{
    private readonly ILogger<PreAuthPostCheck> _logger;

    public PreAuthPostCheck(ILogger<PreAuthPostCheck> logger)
    {
        _logger = logger;
    }

    public Task ExecuteAsync(IRadiusPipelineExecutionContext context)
    {
        _logger.LogDebug("'{name}' started", nameof(PreAuthPostCheck));

        if (context.AuthenticationState.SecondFactorStatus is AuthenticationStatus.Accept or AuthenticationStatus.Bypass)
        {
            _logger.LogDebug("Pre-auth post-check continued pipeline for '{user}' at '{domain}'.", context.RequestPacket.UserName, context.LdapSchema!.NamingContext.StringRepresentation);
            return Task.CompletedTask;
        }

        context.ExecutionState.Terminate();
        _logger.LogDebug("Pre-auth post-check terminated pipeline for '{user}' at '{domain}'.", context.RequestPacket.UserName, context.LdapSchema!.NamingContext.StringRepresentation);
        return Task.CompletedTask;
    }
}