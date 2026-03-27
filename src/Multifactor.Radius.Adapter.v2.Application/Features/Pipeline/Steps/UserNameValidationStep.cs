using Microsoft.Extensions.Logging;
using Multifactor.Radius.Adapter.v2.Application.Core.Enum;
using Multifactor.Radius.Adapter.v2.Application.Core.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.LoadLdapForest.Models;

namespace Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.Steps;

internal sealed class UserNameValidationStep : IRadiusPipelineStep
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
        
        if (!IsPermittedSuffix(identity.GetUpnSuffix(), serverSettings.IncludedSuffixes, serverSettings.ExcludedSuffixes, context.ForestMetadata))
        {
            TerminateWithError(context, "UPN suffix is not permitted.");
            _logger.LogWarning("UPN suffix is not permitted. Provided name: {name}", userName);
        }

        return Task.CompletedTask;
    }

    private bool IsPermittedSuffix(
        string domain,
        IReadOnlyList<string> includedSuffixes,
        IReadOnlyList<string> excludedSuffixes,
        IForestMetadata? forestMetadata)
    {
        if (string.IsNullOrWhiteSpace(domain))
            throw new ArgumentNullException(nameof(domain));

        if (forestMetadata != null)
        {
            var existsInForest = forestMetadata.UpnSuffixes.ContainsKey(domain) ||
                                forestMetadata.UpnSuffixes.Keys.Any(s => domain.EndsWith(s));

            if (!existsInForest)
            {
                _logger.LogDebug("UPN suffix '{suffix}' not found in forest metadata", domain);
                return false;
            }
        }

        if (includedSuffixes != null && includedSuffixes.Count > 0)
            return includedSuffixes.Any(included => included.Equals(domain, StringComparison.CurrentCultureIgnoreCase));

        if (excludedSuffixes != null && excludedSuffixes.Count > 0)
            return excludedSuffixes.All(excluded => !excluded.Equals(domain, StringComparison.CurrentCultureIgnoreCase));

        return true;
    }

    private static void TerminateWithError(RadiusPipelineContext context, string replyMessage)
    {
        context.FirstFactorStatus = AuthenticationStatus.Reject;
        context.SecondFactorStatus = AuthenticationStatus.Awaiting;
        context.ResponseInformation.ReplyMessage = replyMessage;
        context.Terminate();
    }
}