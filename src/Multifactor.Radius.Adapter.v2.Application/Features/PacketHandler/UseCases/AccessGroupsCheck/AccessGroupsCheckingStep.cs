using Microsoft.Extensions.Logging;
using Multifactor.Radius.Adapter.v2.Application.Core.Enum;
using Multifactor.Radius.Adapter.v2.Application.Core.Models;
using Multifactor.Radius.Adapter.v2.Application.Core.Models.Dto;
using Multifactor.Radius.Adapter.v2.Application.SharedPorts;

namespace Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.AccessGroupsCheck;

internal sealed class AccessGroupsCheckingStep : IRadiusPipelineStep
{
    private readonly ICheckMembership _checkMembership;
    private readonly ILogger<AccessGroupsCheckingStep> _logger;
    private const string StepName = nameof(AccessGroupsCheckingStep);

    public AccessGroupsCheckingStep(ICheckMembership checkMembership,
        ILogger<AccessGroupsCheckingStep> logger)
    {
        _logger = logger;
        _checkMembership = checkMembership;
    }

    public Task ExecuteAsync(RadiusPipelineContext context)
    {
        _logger.LogDebug("'{name}' started", StepName);
        ArgumentNullException.ThrowIfNull(context, nameof(context));
        ArgumentNullException.ThrowIfNull(context.LdapConfiguration, nameof(context.LdapConfiguration));

        if (ShouldSkipStep(context))
            return Task.CompletedTask;
        
        ArgumentNullException.ThrowIfNull(context.LdapProfile, nameof(context.LdapProfile));
        
        var userIdentity = new UserIdentity(context.RequestPacket.UserName);
        var domainInfo = context.ForestMetadata?.DetermineForestDomain(userIdentity);
        var accessGroup = context.LdapConfiguration.AccessGroups;
        var request = MembershipDto.FromContext(context, accessGroup, domainInfo);
        var isMember = context.LdapProfile.MemberOf.Intersect(accessGroup).Any();
        if (!isMember && !context.LdapConfiguration.LoadNestedGroups)
        {
            return isMember ? ProcessPipeline(context) : TerminatePipeline(context);
        }
        isMember = _checkMembership.Execute(request);

        return isMember ? ProcessPipeline(context) : TerminatePipeline(context);
    }

    private Task ProcessPipeline(RadiusPipelineContext context)
    {
        _logger.LogDebug("User '{user}' is member of '{group}'", context.RequestPacket.UserName,
            context.LdapConfiguration?.AccessGroups);
        return Task.CompletedTask;
    }
    
    private Task TerminatePipeline(RadiusPipelineContext context)
    {
        _logger.LogWarning("User '{user}' is not member of any access group. Groups:'{group}'", context.LdapProfile!.Dn,
            context.LdapConfiguration?.AccessGroups);
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