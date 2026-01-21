using Microsoft.Extensions.Logging;
using Multifactor.Radius.Adapter.v2.Application.Configuration.Models.Enum;
using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.Models.Enum;

namespace Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.Steps;

public class PreAuthCheckStep : IRadiusPipelineStep
{
    private readonly ILogger<PreAuthCheckStep> _logger;
    
    public PreAuthCheckStep(ILogger<PreAuthCheckStep> logger)
    {
        _logger = logger;    
    }

    public Task ExecuteAsync(RadiusPipelineContext context)
    {
        _logger.LogDebug("'{name}' started", nameof(PreAuthCheckStep));
        switch (context.ClientConfiguration.PreAuthenticationMethod)
        {
            case PreAuthMode.Otp when context.Passphrase?.Otp == null:
                context.SecondFactorStatus = AuthenticationStatus.Reject;
                _logger.LogError("Pre-auth second factor was rejected: otp code is empty. User '{user:l}' from {host:l}:{port}",
                    context.RequestPacket.UserName, 
                    context.RequestPacket.RemoteEndpoint.Address, 
                    context.RequestPacket.RemoteEndpoint.Port);
                context.Terminate();
                return Task.CompletedTask;
            
            case PreAuthMode.None:
            case PreAuthMode.Otp:
            case PreAuthMode.Any:
                _logger.LogDebug("Pre-auth check for '{user}' is completed.", context.RequestPacket.UserName);
                return Task.CompletedTask;
            
            default:
                throw new NotImplementedException($"Unknown pre-auth method: {context.ClientConfiguration.PreAuthenticationMethod}"); 
        }
    }
}