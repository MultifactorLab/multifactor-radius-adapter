using Microsoft.Extensions.Logging;
using Multifactor.Radius.Adapter.v2.Core;
using Multifactor.Radius.Adapter.v2.Core.Auth;
using Multifactor.Radius.Adapter.v2.Core.Ldap.Identity;
using Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline.Context;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline.Steps;

public class UserNameValidationStep : IRadiusPipelineStep
{
    private readonly ILogger<UserNameValidationStep> _logger;
    
    public UserNameValidationStep(ILogger<UserNameValidationStep> logger)
    {
        _logger = logger;
    }

    public Task ExecuteAsync(IRadiusPipelineExecutionContext context)
    {
        var userName = context.RequestPacket.UserName;
        if (string.IsNullOrWhiteSpace(userName))
            throw new InvalidOperationException("Empty user name");
        
        var serverSettings = context.LdapServerConfiguration;
        
        if (serverSettings is null)
            throw new InvalidOperationException("No LDAP server configuration");
        
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
            return Task.CompletedTask;
        }

        return Task.CompletedTask;
    }

    private void TerminateWithError(IRadiusPipelineExecutionContext context, string replyMessage)
    {
        context.AuthenticationState.FirstFactorStatus = AuthenticationStatus.Reject;
        context.AuthenticationState.SecondFactorStatus = AuthenticationStatus.Reject;
        context.ExecutionState.Terminate();
        context.ResponseInformation.ReplyMessage = replyMessage;
    }
}