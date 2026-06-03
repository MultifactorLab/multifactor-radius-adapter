using Microsoft.Extensions.Logging;
using Multifactor.Radius.Adapter.v2.Application.Core.Enum;
using Multifactor.Radius.Adapter.v2.Application.Core.Models;
using Multifactor.Radius.Adapter.v2.Application.Core.Models.Dto;
using Multifactor.Radius.Adapter.v2.Application.SharedPorts;

namespace Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.DenyGroupsCheck;

public class DenyGroupsCheckingStep : IRadiusPipelineStep
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
 
        var isMember = context.LdapProfile.MemberOf.Intersect(denyGroups).Any();
        if (isMember && !context.LdapConfiguration.LoadNestedGroups)
        {
            _logger.LogInformation(
                "User '{user:l}' is a member of the Deny group in '{domain:l}'. Access rejected.",
                context.RequestPacket.UserName, context.LdapConfiguration.ConnectionString);
            return TerminatePipeline(context);
        }
 
        var request = MembershipDto.FromContext(context, denyGroups, domainInfo);
        isMember = _checkMembership.Execute(request);
 
        if (isMember)
        {
            _logger.LogInformation(
                "User '{user:l}' is a member of the Deny group in '{domain:l}'. Access rejected.",
                context.RequestPacket.UserName, context.LdapConfiguration.ConnectionString);
            return TerminatePipeline(context);
        }
 
        _logger.LogDebug(
            "User '{user:l}' is not a member of any Deny group in '{domain:l}'. Access allowed to proceed.",
            context.RequestPacket.UserName, context.LdapConfiguration.ConnectionString);
        return Task.CompletedTask;
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