using Microsoft.Extensions.Logging;
using Multifactor.Radius.Adapter.v2.Core.Auth;
using Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline.Context;

namespace Multifactor.Radius.Adapter.v2.Core.FirstFactor;

public class NoneFirstFactorProcessor : IFirstFactorProcessor
{
    private readonly ILogger<NoneFirstFactorProcessor> _logger;
    public AuthenticationSource AuthenticationSource => AuthenticationSource.None;

    public NoneFirstFactorProcessor(ILogger<NoneFirstFactorProcessor> logger)
    {
        _logger = logger;
    }
    public Task ProcessFirstFactor(IRadiusPipelineExecutionContext context)
    {
        context.AuthenticationState.FirstFactorStatus = AuthenticationStatus.Accept;
        _logger.LogInformation("Bypass first factor for user '{user:l}' due to '{ff:l}' variant of first factor.", context.RequestPacket.UserName, context.Settings.FirstFactorAuthenticationSource.ToString());
        return Task.CompletedTask;
    }
}