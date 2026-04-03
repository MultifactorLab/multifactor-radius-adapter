using Microsoft.Extensions.Logging;
using Multifactor.Radius.Adapter.v2.Application.Core.Enum;
using Multifactor.Radius.Adapter.v2.Application.Core.Models;

namespace Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.PreAuthPostCheck;

internal sealed class PreAuthPostCheckStep : IRadiusPipelineStep
{
    private readonly ILogger<PreAuthPostCheckStep> _logger;
    private const string StepName = nameof(PreAuthPostCheckStep);
    public PreAuthPostCheckStep(ILogger<PreAuthPostCheckStep> logger)
    {
        _logger = logger;
    }

    public Task ExecuteAsync(RadiusPipelineContext context)
    {
        _logger.LogDebug("'{name}' started", StepName);

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