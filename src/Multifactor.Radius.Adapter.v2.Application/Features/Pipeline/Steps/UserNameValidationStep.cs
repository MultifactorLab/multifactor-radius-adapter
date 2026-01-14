using Microsoft.Extensions.Logging;
using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.Models.Enum;
using Multifactor.Radius.Adapter.v2.Application.Models.Enum;

namespace Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.Steps;

public class UserNameValidationStep : IRadiusPipelineStep
{
    private readonly ILogger<UserNameValidationStep> _logger;
    
    public UserNameValidationStep(ILogger<UserNameValidationStep> logger)
    {
        _logger = logger;
    }

    public Task ExecuteAsync(RadiusPipelineContext context)
    {
        _logger.LogDebug("'{name}' started", nameof(UserNameValidationStep));
        
        var userName = context.RequestPacket.UserName;
        if (string.IsNullOrWhiteSpace(userName))
        {
            _logger.LogDebug("User name is empty");
            return Task.CompletedTask;
        }

        var serverSettings = context.LdapConfiguration;

        if (serverSettings is null)
        {
            _logger.LogDebug("No LDAP server configuration provided. User name validation will be skipped.");
            return Task.CompletedTask;
        }

        var identity = new UserIdentity(userName);

        if (serverSettings.RequiresUpn && identity.Format != UserIdentityFormat.UserPrincipalName)
        {
            TerminateWithError(context, "User name in UPN format is required.");
            _logger.LogWarning("User name in UPN format is required. Provided name: {name}", userName);
            return Task.CompletedTask;
        }

        if (identity.Format != UserIdentityFormat.UserPrincipalName)
            return Task.CompletedTask;
        
        if (!IsPermittedSuffix(identity.GetUpnSuffix(), serverSettings.IncludedDomains, serverSettings.ExcludedDomains))
        {
            TerminateWithError(context, "UPN suffix is not permitted.");
            _logger.LogWarning("UPN suffix is not permitted. Provided name: {name}", userName);
        }

        return Task.CompletedTask;
    }
    
    private static bool IsPermittedSuffix(string domain, IReadOnlyList<string> includedDomains, IReadOnlyList<string> excludedDomains)
    {
        if (string.IsNullOrWhiteSpace(domain)) throw new ArgumentNullException(nameof(domain));

        if (includedDomains.Count > 0)
            return includedDomains.Any(included => included.Equals(domain, StringComparison.CurrentCultureIgnoreCase));

        if (excludedDomains.Count > 0)
            return excludedDomains.All(excluded => !excluded.Equals(domain, StringComparison.CurrentCultureIgnoreCase));
        
        return true;
    }

    private static void TerminateWithError(RadiusPipelineContext context, string replyMessage)
    {
        context.FirstFactorStatus = AuthenticationStatus.Reject;
        context.SecondFactorStatus = AuthenticationStatus.Awaiting;
        context.Terminate();
        context.ResponseInformation.ReplyMessage = replyMessage;
    }
}