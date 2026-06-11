using Microsoft.Extensions.Logging;
using Multifactor.Radius.Adapter.v2.Application.Core.Enum;
using Multifactor.Radius.Adapter.v2.Application.Core.Models;
using Multifactor.Radius.Adapter.v2.Application.Core.Models.Dto;
using Multifactor.Radius.Adapter.v2.Application.SharedPorts;

namespace Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.DenyGroupsCheck;

internal sealed class DenyGroupsCheckingStep : IRadiusPipelineStep
{
    private readonly ICheckMembership _checkMembership;
    private readonly ILogger<DenyGroupsCheckingStep> _logger;
    private const string StepName = nameof(DenyGroupsCheckingStep);
 
    public DenyGroupsCheckingStep(ICheckMembership checkMembership,
        ILogger<DenyGroupsCheckingStep> logger)
    {
        _checkMembership = checkMembership;
        _logger = logger;
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
        var denyGroups = context.LdapConfiguration.DenyGroups;
 
        // Fast path: check flat MemberOf before querying nested groups
        var matchedGroup = context.LdapProfile.MemberOf.Intersect(denyGroups).FirstOrDefault();
        if (matchedGroup is not null && !context.LdapConfiguration.LoadNestedGroups)
        {
            LogDenied(context.RequestPacket.UserName, matchedGroup.ToString());
            return TerminatePipeline(context);
        }
 
        var request = MembershipDto.FromContext(context, denyGroups, domainInfo);
        var isMember = _checkMembership.Execute(request);
 
        if (isMember)
        {
            // For nested groups we don't have the exact matched group name from ICheckMembership,
            // so we report the configured deny groups for full context
            var groupNames = string.Join(", ", denyGroups);
            LogDenied(context.RequestPacket.UserName, groupNames);
            return TerminatePipeline(context);
        }
 
        _logger.LogDebug(
            "User '{user:l}' is not a member of any Deny group in '{domain:l}'. Access allowed to proceed.",
            context.RequestPacket.UserName, context.LdapConfiguration.ConnectionString);
        return Task.CompletedTask;
    }
 
    private void LogDenied(string userName, string groupName)
    {
        _logger.LogInformation(
            "User '{user:l}' authentication failed. Denied by group '{group:l}'.",
            userName, groupName);
    }
 
    private static Task TerminatePipeline(RadiusPipelineContext context)
    {
        context.FirstFactorStatus = AuthenticationStatus.Reject;
        context.SecondFactorStatus = AuthenticationStatus.Reject;
        context.Terminate();
        return Task.CompletedTask;
    }
 
    private bool ShouldSkipStep(RadiusPipelineContext context)
    {
        return NoDenyGroups(context) || UnsupportedAccountType(context);
    }
 
    private bool NoDenyGroups(RadiusPipelineContext context)
    {
        var noGroups = context.LdapConfiguration!.DenyGroups.Count == 0;
        if (!noGroups)
            return false;
 
        _logger.LogDebug("No deny groups were specified.");
        return true;
    }
 
    private bool UnsupportedAccountType(RadiusPipelineContext context)
    {
        if (context.IsDomainAccount)
            return false;
 
        var packet = context.RequestPacket;
        _logger.LogInformation(
            "User '{user}' used '{accountType}' account to log in. Deny groups checking is skipped.",
            packet.UserName,
            packet.AccountType);
        return true;
    }
}