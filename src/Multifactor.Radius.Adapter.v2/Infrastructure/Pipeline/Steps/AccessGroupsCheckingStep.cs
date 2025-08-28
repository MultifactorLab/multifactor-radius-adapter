using Microsoft.Extensions.Logging;
using Multifactor.Core.Ldap.Name;
using Multifactor.Radius.Adapter.v2.Core.Auth;
using Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline.Context;
using Multifactor.Radius.Adapter.v2.Services.Ldap;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline.Steps;

public class AccessGroupsCheckingStep : IRadiusPipelineStep
{
    private readonly ILdapGroupService _ldapGroupService;
    private readonly ILogger<AccessGroupsCheckingStep> _logger;

    public AccessGroupsCheckingStep(
        ILdapGroupService ldapGroupService,
        ILogger<AccessGroupsCheckingStep> logger)
    {
        _ldapGroupService = ldapGroupService;
        _logger = logger;
    }

    public Task ExecuteAsync(IRadiusPipelineExecutionContext context)
    {
        _logger.LogDebug("'{name}' started", nameof(AccessGroupsCheckingStep));
        ArgumentNullException.ThrowIfNull(context, nameof(context));
        ArgumentNullException.ThrowIfNull(context.LdapServerConfiguration, nameof(context.LdapServerConfiguration));
        ArgumentNullException.ThrowIfNull(context.LdapSchema, nameof(context.LdapSchema));

        var serverConfig = context.LdapServerConfiguration;

        if (ShouldSkipStep(context))
            return Task.CompletedTask;
        
        ArgumentNullException.ThrowIfNull(context.UserLdapProfile, nameof(context.UserLdapProfile));
        
        var accessGroupsDns = serverConfig.AccessGroups.Select(x => new DistinguishedName(x)).ToArray();
        var request = GetMembershipRequest(context, accessGroupsDns);
        var isMember = _ldapGroupService.IsMemberOf(request);

        return isMember ? Task.CompletedTask : TerminatePipeline(context);
    }

    private MembershipRequest GetMembershipRequest(IRadiusPipelineExecutionContext context,
        DistinguishedName[] accessGroupNames) => new(context, accessGroupNames);

    private Task TerminatePipeline(IRadiusPipelineExecutionContext context)
    {
        _logger.LogWarning("User '{user}' is not member of any access group of the '{connectionString}'.",
            context.UserLdapProfile!.Dn, context.LdapServerConfiguration!.ConnectionString);
        context.AuthenticationState.FirstFactorStatus = AuthenticationStatus.Reject;
        context.AuthenticationState.SecondFactorStatus = AuthenticationStatus.Reject;
        context.ExecutionState.Terminate();
        return Task.CompletedTask;
    }

    private bool ShouldSkipStep(IRadiusPipelineExecutionContext context)
    {
        return NoAccessGroups(context) || UnsupportedAccountType(context);
    }
    
    private bool NoAccessGroups(IRadiusPipelineExecutionContext config)
    {
        var noGroups = config.LdapServerConfiguration!.AccessGroups.Count == 0;
        
        if (!noGroups)
            return false;
        
        _logger.LogDebug("No access groups were specified.");
        return true;
    }

    private bool UnsupportedAccountType(IRadiusPipelineExecutionContext context)
    {
        if (context.IsDomainAccount)
            return false;
        
        var packet = context.RequestPacket;
        _logger.LogInformation(
            "User '{user}' used '{accountType}' account to log in. Access groups checking is skipped.",
            packet.UserName,
            packet.AccountType);

        return true;
    }
}