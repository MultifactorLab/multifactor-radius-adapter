using Microsoft.Extensions.Logging;
using Multifactor.Core.Ldap.Name;
using Multifactor.Radius.Adapter.v2.Application.Pipeline.Steps.Interfaces;
using Multifactor.Radius.Adapter.v2.Domain.Auth;
using Multifactor.Radius.Adapter.v2.Infrastructure.Ldap.Dto;
using Multifactor.Radius.Adapter.v2.Infrastructure.Ldap.Interface;
using Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline;

namespace Multifactor.Radius.Adapter.v2.Application.Pipeline.Steps;

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

    public Task ExecuteAsync(RadiusPipelineExecutionContext context)
    {
        _logger.LogDebug("'{name}' started", nameof(AccessGroupsCheckingStep));
        ArgumentNullException.ThrowIfNull(context, nameof(context));
        ArgumentNullException.ThrowIfNull(context.LdapServerConfiguration, nameof(context.LdapServerConfiguration));
        ArgumentNullException.ThrowIfNull(context.LdapSchema, nameof(context.LdapSchema));

        var serverConfig = context.LdapServerConfiguration;

        if (ShouldSkipStep(context))
            return Task.CompletedTask;
        
        ArgumentNullException.ThrowIfNull(context.UserLdapProfile, nameof(context.UserLdapProfile));
        
        var accessGroupsDns = serverConfig.AccessGroups.ToArray();
        var request = GetMembershipRequest(context, accessGroupsDns);
        var isMember = _ldapGroupService.IsMemberOf(request);

        return isMember ? Task.CompletedTask : TerminatePipeline(context);
    }

    private MembershipRequest GetMembershipRequest(RadiusPipelineExecutionContext context,
        DistinguishedName[] accessGroupNames) => new(context, accessGroupNames);

    private Task TerminatePipeline(RadiusPipelineExecutionContext context)
    {
        _logger.LogWarning("User '{user}' is not member of any access group of the '{connectionString}'.",
            context.UserLdapProfile!.Dn, context.LdapServerConfiguration!.ConnectionString);
        context.AuthenticationState.FirstFactorStatus = AuthenticationStatus.Reject;
        context.AuthenticationState.SecondFactorStatus = AuthenticationStatus.Reject;
        context.ExecutionState.Terminate();
        return Task.CompletedTask;
    }

    private bool ShouldSkipStep(RadiusPipelineExecutionContext context)
    {
        return NoAccessGroups(context) || UnsupportedAccountType(context);
    }
    
    private bool NoAccessGroups(RadiusPipelineExecutionContext config)
    {
        var noGroups = config.LdapServerConfiguration!.AccessGroups.Count == 0;
        
        if (!noGroups)
            return false;
        
        _logger.LogDebug("No access groups were specified.");
        return true;
    }

    private bool UnsupportedAccountType(RadiusPipelineExecutionContext context)
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