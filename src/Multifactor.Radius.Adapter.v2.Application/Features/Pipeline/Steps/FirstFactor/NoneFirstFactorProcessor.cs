using Microsoft.Extensions.Logging;
using Multifactor.Radius.Adapter.v2.Application.Core.Enum;
using Multifactor.Radius.Adapter.v2.Application.Core.Models;

namespace Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.Steps.FirstFactor;

public class NoneFirstFactorProcessor : IFirstFactorProcessor
{
    private readonly ILogger<NoneFirstFactorProcessor> _logger;
    public AuthenticationSource AuthenticationSource => AuthenticationSource.None;

    public NoneFirstFactorProcessor(ILogger<NoneFirstFactorProcessor> logger)
    {
        _logger = logger;
    }
    public Task ProcessFirstFactor(RadiusPipelineContext context)
    {
        context.FirstFactorStatus = AuthenticationStatus.Accept;
        _logger.LogInformation("Bypass first factor for user '{user:l}' due to '{ff:l}' variant of first factor.", context.RequestPacket.UserName, context.ClientConfiguration.FirstFactorAuthenticationSource.ToString());
        return Task.CompletedTask;
    }
}