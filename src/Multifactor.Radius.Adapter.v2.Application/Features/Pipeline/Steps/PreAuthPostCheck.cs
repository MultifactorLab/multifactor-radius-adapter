using Microsoft.Extensions.Logging;
using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.Models;
using Multifactor.Radius.Adapter.v2.Application.Models.Enum;

namespace Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.Steps;

public class PreAuthPostCheck : IRadiusPipelineStep
{
    private readonly ILogger<PreAuthPostCheck> _logger;

    public PreAuthPostCheck(ILogger<PreAuthPostCheck> logger)
    {
        _logger = logger;
    }

    public Task ExecuteAsync(RadiusPipelineContext context)
    {
        _logger.LogDebug("'{name}' started", nameof(PreAuthPostCheck));

        if (context.SecondFactorStatus is AuthenticationStatus.Accept or AuthenticationStatus.Bypass)
        {
            _logger.LogDebug("Pre-auth post-check continued pipeline for '{user}' at '{domain}'.", context.RequestPacket.UserName, context.LdapSchema?.NamingContext.StringRepresentation);
            return Task.CompletedTask;
        }

        context.Terminate();
        _logger.LogDebug("Pre-auth post-check terminated pipeline for '{user}' at '{domain}'.", context.RequestPacket.UserName, context.LdapSchema?.NamingContext.StringRepresentation);
        return Task.CompletedTask;
    }
}