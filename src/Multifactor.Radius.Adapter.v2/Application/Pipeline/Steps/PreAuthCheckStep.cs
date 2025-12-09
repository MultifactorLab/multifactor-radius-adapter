using Microsoft.Extensions.Logging;
using Multifactor.Radius.Adapter.v2.Application.Pipeline.Steps.Interfaces;
using Multifactor.Radius.Adapter.v2.Domain.Auth;
using Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline;

namespace Multifactor.Radius.Adapter.v2.Application.Pipeline.Steps;

public class PreAuthCheckStep : IRadiusPipelineStep
{
    private readonly ILogger<PreAuthCheckStep> _logger;
    
    public PreAuthCheckStep(ILogger<PreAuthCheckStep> logger)
    {
        _logger = logger;    
    }

    public Task ExecuteAsync(RadiusPipelineExecutionContext context)
    {
        _logger.LogDebug("'{name}' started", nameof(PreAuthCheckStep));
        switch (context.PreAuthnMode.Mode)
        {
            case PreAuthMode.Otp when context.Passphrase.Otp == null:
                context.AuthenticationState.SecondFactorStatus = AuthenticationStatus.Reject;
                _logger.LogError("Pre-auth second factor was rejected: otp code is empty. User '{user:l}' from {host:l}:{port}",
                    context.RequestPacket.UserName, 
                    context.RemoteEndpoint.Address, 
                    context.RemoteEndpoint.Port);
                context.ExecutionState.Terminate();
                return Task.CompletedTask;
            
            case PreAuthMode.None:
            case PreAuthMode.Otp:
            case PreAuthMode.Any:
                _logger.LogDebug("Pre-auth check for '{user}' is completed.", context.RequestPacket.UserName);
                return Task.CompletedTask;
            
            default:
                throw new NotImplementedException($"Unknown pre-auth method: {context.PreAuthnMode.Mode}"); 
        }
    }
}