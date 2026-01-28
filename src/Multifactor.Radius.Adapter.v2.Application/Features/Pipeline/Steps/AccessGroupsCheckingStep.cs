using Microsoft.Extensions.Logging;
using Multifactor.Radius.Adapter.v2.Application.Features.Ldap;
using Multifactor.Radius.Adapter.v2.Application.Features.Ldap.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.Ldap.Ports;
using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.Models.Enum;

namespace Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.Steps;

public class AccessGroupsCheckingStep : IRadiusPipelineStep
{
    private readonly ILdapAdapter _ldapAdapter;
    private readonly ILogger<AccessGroupsCheckingStep> _logger;

    public AccessGroupsCheckingStep(
        ILdapAdapter ldapAdapter,
        ILogger<AccessGroupsCheckingStep> logger)
    {
        _ldapAdapter = ldapAdapter;
        _logger = logger;
    }

    public Task ExecuteAsync(RadiusPipelineContext context)
    {
        _logger.LogDebug("'{name}' started", nameof(AccessGroupsCheckingStep));
        ArgumentNullException.ThrowIfNull(context, nameof(context));
        ArgumentNullException.ThrowIfNull(context.LdapConfiguration, nameof(context.LdapConfiguration));
        ArgumentNullException.ThrowIfNull(context.LdapSchema, nameof(context.LdapSchema));

        if (ShouldSkipStep(context))
            return Task.CompletedTask;
        
        ArgumentNullException.ThrowIfNull(context.LdapProfile, nameof(context.LdapProfile));
        var accessGroup = context.LdapConfiguration.AccessGroups;
        var request = MembershipRequest.FromContext(context, accessGroup);
        var isMember = context.LdapProfile.MemberOf.Intersect(accessGroup).Any() || _ldapAdapter.IsMemberOf(request);

        return isMember ? Task.CompletedTask : TerminatePipeline(context);
    }

    private Task TerminatePipeline(RadiusPipelineContext context)
    {
        _logger.LogWarning("User '{user}' is not member of any access group of the '{connectionString}'.",
            context.LdapProfile!.Dn, context.LdapConfiguration!.ConnectionString);
        context.FirstFactorStatus = AuthenticationStatus.Reject;
        context.SecondFactorStatus = AuthenticationStatus.Reject;
        context.Terminate();
        return Task.CompletedTask;
    }

    private bool ShouldSkipStep(RadiusPipelineContext context)
    {
        return NoAccessGroups(context) || UnsupportedAccountType(context);
    }
    
    private bool NoAccessGroups(RadiusPipelineContext config)
    {
        var noGroups = config.LdapConfiguration!.AccessGroups.Count == 0;
        
        if (!noGroups)
            return false;
        
        _logger.LogDebug("No access groups were specified.");
        return true;
    }

    private bool UnsupportedAccountType(RadiusPipelineContext context)
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