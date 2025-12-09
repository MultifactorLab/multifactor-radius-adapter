using Microsoft.Extensions.Logging;
using Multifactor.Radius.Adapter.v2.Application.Pipeline.Steps.Interfaces;
using Multifactor.Radius.Adapter.v2.Domain;
using Multifactor.Radius.Adapter.v2.Domain.Auth;
using Multifactor.Radius.Adapter.v2.Domain.Ldap.Identity;
using Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline;

namespace Multifactor.Radius.Adapter.v2.Application.Pipeline.Steps;

public class UserNameValidationStep : IRadiusPipelineStep
{
    private readonly ILogger<UserNameValidationStep> _logger;
    
    public UserNameValidationStep(ILogger<UserNameValidationStep> logger)
    {
        _logger = logger;
    }

    public Task ExecuteAsync(RadiusPipelineExecutionContext context)
    {
        _logger.LogDebug("'{name}' started", nameof(UserNameValidationStep));
        
        var userName = context.RequestPacket.UserName;
        if (string.IsNullOrWhiteSpace(userName))
        {
            _logger.LogDebug("User name is empty");
            return Task.CompletedTask;
        }

        var serverSettings = context.LdapServerConfiguration;

        if (serverSettings is null)
        {
            _logger.LogDebug("No LDAP server configuration provided. User name validation will be skipped.");
            return Task.CompletedTask;
        }

        var identity = new UserIdentity(userName);

        if (serverSettings.UpnRequired && identity.Format != UserIdentityFormat.UserPrincipalName)
        {
            TerminateWithError(context, "User name in UPN format is required.");
            _logger.LogWarning("User name in UPN format is required. Provided name: {name}", userName);
            return Task.CompletedTask;
        }

        if (identity.Format != UserIdentityFormat.UserPrincipalName)
            return Task.CompletedTask;
        
        var suffix = Utils.GetUpnSuffix(identity);
        var isPermitted = serverSettings.SuffixesPermissions.IsPermitted(suffix);
        if (!isPermitted)
        {
            TerminateWithError(context, "UPN suffix is not permitted.");
            _logger.LogWarning("UPN suffix is not permitted. Provided name: {name}", userName);
        }

        return Task.CompletedTask;
    }

    private static void TerminateWithError(RadiusPipelineExecutionContext context, string replyMessage)
    {
        context.AuthenticationState.FirstFactorStatus = AuthenticationStatus.Reject;
        context.AuthenticationState.SecondFactorStatus = AuthenticationStatus.Awaiting;
        context.ExecutionState.Terminate();
        context.ResponseInformation.ReplyMessage = replyMessage;
    }
}