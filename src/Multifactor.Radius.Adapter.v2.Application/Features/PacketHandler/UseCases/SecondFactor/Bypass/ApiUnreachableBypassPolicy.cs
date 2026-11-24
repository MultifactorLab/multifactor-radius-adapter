using System.Net;
using Microsoft.Extensions.Logging;
using Multifactor.Core.Ldap.Name;
using Multifactor.Radius.Adapter.v2.Application.Core.Enum;
using Multifactor.Radius.Adapter.v2.Application.Core.Models;
using Multifactor.Radius.Adapter.v2.Application.Core.Models.Dto;
using Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.SecondFactor.Multifactor;
using Multifactor.Radius.Adapter.v2.Application.SharedPorts;

namespace Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.SecondFactor.Bypass;

/// <summary>
/// Decides whether the second factor may be skipped when the MULTIFACTOR API is unreachable.
/// When set, bypass-policy takes priority over bypass-second-factor-when-api-unreachable.
/// </summary>
public sealed class ApiUnreachableBypassPolicy
{
    private readonly ICheckMembership _checkMembership;
    private readonly ILogger<ApiUnreachableBypassPolicy> _logger;

    public ApiUnreachableBypassPolicy(ICheckMembership checkMembership, ILogger<ApiUnreachableBypassPolicy> logger)
    {
        _checkMembership = checkMembership;
        _logger = logger;
    }

    public bool IsBypassAllowed(RadiusPipelineContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var policy = context.ClientConfiguration.BypassPolicy;
        return policy is null ? IsBypassAllowedLegacy(context) : IsBypassAllowedPolicy(context, policy);
    }

    private bool IsBypassAllowedLegacy(RadiusPipelineContext context)
    {
        if (!context.ClientConfiguration.BypassSecondFactorWhenApiUnreachable)
        {
            LogDecision(context, "global bypass", "-", null, null, false,
                "'bypass-second-factor-when-api-unreachable' is false");
            return false;
        }

        var groups = context.LdapConfiguration?.BypassSecondFactorWhenApiUnreachableGroups;
        if (groups is null || !groups.Any())
        {
            LogDecision(context, "global bypass", "-", null, null, true,
                "'bypass-second-factor-when-api-unreachable' is true");
            return true;
        }

        var userIdentity = new UserIdentity(context.RequestPacket.UserName);
        var domainInfo = context.ForestMetadata?.DetermineForestDomain(userIdentity);
        var isMember = _checkMembership.Execute(MembershipDto.FromContext(context, groups, domainInfo));

        LogDecision(context, "bypass-second-factor-when-api-unreachable-groups", "-", null, isMember, isMember,
            isMember
                ? "user is a member of 'bypass-second-factor-when-api-unreachable-groups'"
                : "user is not a member of 'bypass-second-factor-when-api-unreachable-groups'");
        return isMember;
    }

    private bool IsBypassAllowedPolicy(RadiusPipelineContext context, BypassPolicy policy)
    {
        var sourceIp = RequestDataExtractor.GetSourceIp(context);
        var groups = policy.UsesGroups ? context.LdapConfiguration?.BypassSecondFactorWhenApiUnreachableGroups ?? [] : [];
        bool? ipMatched = null;
        bool? groupMatched = null;

        switch (policy.Mode)
        {
            case BypassPolicyMode.IpOnly:
            {
                ipMatched = IsIpAllowed(policy.IpWhitelist, sourceIp);
                break;
            }
            case BypassPolicyMode.GroupOnly:
            {
                groupMatched = IsGroupMember(context, groups);
                break;
            }
            case BypassPolicyMode.Or:
            {
                ipMatched = IsIpAllowed(policy.IpWhitelist, sourceIp);
                if (ipMatched == false)
                {
                    groupMatched = IsGroupMember(context, groups);
                }
                break;
            }
            case BypassPolicyMode.And:
            {
                ipMatched = IsIpAllowed(policy.IpWhitelist, sourceIp);
                if (ipMatched == true)
                {
                    groupMatched = IsGroupMember(context, groups);
                }
                break;
            }
        }

        var allowed = policy.Mode == BypassPolicyMode.And
            ? ipMatched == true && groupMatched == true
            : ipMatched == true || groupMatched == true;
        string reason;
        if (policy.IpWhitelist.Count == 0 && groups.Count == 0)
        {
            reason = "'bypass-ip-whitelist' and 'bypass-second-factor-when-api-unreachable-groups' are empty";
        }
        else
        {
            reason = allowed ? "request matches the policy" : "request does not match the policy";
        }

        LogDecision(context, "[bypass] policy", BypassPolicy.ToConfigValue(policy.Mode), ipMatched, groupMatched, allowed, reason);
        return allowed;
    }

    private static bool IsIpAllowed(IReadOnlyList<IpEntry> whitelist, IPAddress? sourceIp)
    {
        return sourceIp is not null && IpEntry.Matches(whitelist, sourceIp);
    }

    private bool IsGroupMember(RadiusPipelineContext context, IReadOnlyList<DistinguishedName> groups)
    {
        if (groups.Count == 0 || !context.IsDomainAccount || context.LdapConfiguration is null || context.LdapProfile is null)
        {
            return false;
        }

        try
        {
            if (context.LdapProfile.MemberOf.Intersect(groups).Any())
            {
                return true;
            }

            if (!context.LdapConfiguration.LoadNestedGroups)
            {
                return false;
            }

            var domainInfo = context.ForestMetadata?.DetermineForestDomain(new UserIdentity(context.RequestPacket.UserName));
            return _checkMembership.Execute(MembershipDto.FromContext(context, groups, domainInfo));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to check membership in 'bypass-second-factor-when-api-unreachable-groups' for user '{user:l}' at '{domain:l}', the user is treated as not a member",
                context.RequestPacket.UserName, context.LdapConfiguration.ConnectionString);
            return false;
        }
    }

    private void LogDecision(RadiusPipelineContext context, string mechanism, string policy, bool? ipMatched, bool? groupMatched,
        bool allowed, string reason)
    {
        _logger.LogInformation(
            "Second factor bypass decision for user '{user:l}': source IP: '{sourceIp}', API status: unreachable, mechanism: '{mechanism:l}', " +
            "policy: {policy:l}, IP check: {ipCheck:l}, LDAP group check: {groupCheck:l}, load-nested-groups: {loadNestedGroups}, decision: {decision:l}, " +
            "reason: {reason:l}",
            context.RequestPacket.UserName,
            RequestDataExtractor.GetSourceIp(context),
            mechanism,
            policy,
            FormatCheck(ipMatched),
            FormatCheck(groupMatched),
            groupMatched is null ? "-" : context.LdapConfiguration?.LoadNestedGroups,
            allowed ? "Allow" : "Deny",
            reason);

        return;

        static string FormatCheck(bool? matched)
        {
            return matched switch
            {
                true => "match",
                false => "no match",
                null => "not checked"
            };
        }
    }
}
