using System.Net;
using Moq;
using Multifactor.Radius.Adapter.v2.Application.Core.Enum;
using Multifactor.Radius.Adapter.v2.Application.Core.Models.Dto;
using Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.SecondFactor.Multifactor.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.SecondFactor.Multifactor.Models.Enum;
using Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.SecondFactor.Multifactor.Ports;
using static Multifactor.Radius.Adapter.v2.Tests.Fixtures.BypassTestData;

namespace Multifactor.Radius.Adapter.v2.Tests.Application.Pipeline.SecondFactor;

public class ApiUnreachableBypassPolicyTests
{
    [Theory]
    [InlineData("OR", true, true, AuthenticationStatus.Bypass)]
    [InlineData("OR", true, false, AuthenticationStatus.Bypass)]
    [InlineData("OR", false, true, AuthenticationStatus.Bypass)]
    [InlineData("OR", false, false, AuthenticationStatus.Reject)]
    [InlineData("AND", true, true, AuthenticationStatus.Bypass)]
    [InlineData("AND", true, false, AuthenticationStatus.Reject)]
    [InlineData("AND", false, true, AuthenticationStatus.Reject)]
    [InlineData("AND", false, false, AuthenticationStatus.Reject)]
    [InlineData("IP_ONLY", true, false, AuthenticationStatus.Bypass)]
    [InlineData("IP_ONLY", false, true, AuthenticationStatus.Reject)]
    [InlineData("GROUP_ONLY", false, true, AuthenticationStatus.Bypass)]
    [InlineData("GROUP_ONLY", true, false, AuthenticationStatus.Reject)]
    public async Task PolicyFollowsDecisionMatrix(string policy, bool ipMatches, bool isMember, AuthenticationStatus expected)
    {
        var context = Context(ipMatches ? AllowedIp : ForeignIp, Configuration(policy), Ldap());
        var service = Service(membership: Membership(isMember));

        Assert.Equal(expected, (await service.CreateSecondFactorRequestAsync(context, false)).Code);
        Assert.Equal(expected, (await service.SendChallengeAsync(context, false, "request-id", "otp")).Code);
    }

    [Theory]
    [InlineData("192.168.0.5", AuthenticationStatus.Bypass)]
    [InlineData("192.168.0.10", AuthenticationStatus.Bypass)]
    [InlineData("10.0.0.255", AuthenticationStatus.Bypass)]
    [InlineData("192.168.0.6", AuthenticationStatus.Reject)]
    [InlineData("10.0.1.0", AuthenticationStatus.Reject)]
    public async Task IpOnlyMatchesSingleIpsRangesAndCidr(string address, AuthenticationStatus expected)
    {
        var context = Context(address, Configuration("IP_ONLY"));
        Assert.Equal(expected, (await Service().CreateSecondFactorRequestAsync(context, false)).Code);
    }

    [Fact]
    public async Task IpOnlyDoesNotQueryLdap()
    {
        var membership = Membership(true);
        var context = Context(AllowedIp, Configuration("IP_ONLY"), Ldap());
        Assert.Equal(AuthenticationStatus.Bypass, (await Service(membership: membership).CreateSecondFactorRequestAsync(context, false)).Code);
        membership.Verify(x => x.Execute(It.IsAny<MembershipDto>()), Times.Never);
    }

    [Fact]
    public async Task OrSkipsLdapWhenIpMatches()
    {
        var membership = Membership(false);
        var context = Context(AllowedIp, Configuration("OR"), Ldap());
        Assert.Equal(AuthenticationStatus.Bypass, (await Service(membership: membership).CreateSecondFactorRequestAsync(context, false)).Code);
        membership.Verify(x => x.Execute(It.IsAny<MembershipDto>()), Times.Never);
    }

    [Fact]
    public async Task EmptyWhitelistsDenyBypass()
    {
        var config = Configuration("OR", ipWhitelist: null);
        var context = Context(AllowedIp, config, Ldap(apiUnreachableGroups: []));
        Assert.Equal(AuthenticationStatus.Reject, (await Service(membership: Membership(true)).CreateSecondFactorRequestAsync(context, false)).Code);
    }

    // --- LDAP ---

    [Theory]
    [InlineData("OR", ForeignIp, AuthenticationStatus.Reject)]
    [InlineData("AND", AllowedIp, AuthenticationStatus.Reject)]
    [InlineData("GROUP_ONLY", ForeignIp, AuthenticationStatus.Reject)]
    [InlineData("OR", AllowedIp, AuthenticationStatus.Bypass)]
    public async Task LdapErrorIsTreatedAsNotMember(string policy, string address, AuthenticationStatus expected)
    {
        var context = Context(address, Configuration(policy), Ldap(), memberOf: [OtherGroup]);
        Assert.Equal(expected, (await Service(membership: FailingMembership()).CreateSecondFactorRequestAsync(context, false)).Code);
    }

    [Fact]
    public async Task DirectMembershipMatchesWithoutLdapQuery()
    {
        var membership = Membership(false);
        var context = Context(ForeignIp, Configuration("GROUP_ONLY"), Ldap(loadNestedGroups: false), memberOf: [AdminsGroup]);
        Assert.Equal(AuthenticationStatus.Bypass, (await Service(membership: membership).CreateSecondFactorRequestAsync(context, false)).Code);
        membership.Verify(x => x.Execute(It.IsAny<MembershipDto>()), Times.Never);
    }

    [Theory]
    [InlineData(true, AuthenticationStatus.Bypass)]
    [InlineData(false, AuthenticationStatus.Reject)]
    public async Task NestedMembershipIsCheckedOnlyWithLoadNestedGroups(bool loadNestedGroups, AuthenticationStatus expected)
    {
        var membership = Membership(true);
        var context = Context(ForeignIp, Configuration("GROUP_ONLY"), Ldap(loadNestedGroups: loadNestedGroups), memberOf: [OtherGroup]);
        Assert.Equal(expected, (await Service(membership: membership).CreateSecondFactorRequestAsync(context, false)).Code);
        membership.Verify(x => x.Execute(It.IsAny<MembershipDto>()), loadNestedGroups ? Times.Once() : Times.Never());
    }

    [Fact]
    public async Task PolicyOverridesDisabledGlobalBypass()
    {
        var config = Configuration("IP_ONLY");
        config.AppSettings.BypassSecondFactorWhenApiUnreachable = false;
        Assert.Equal(AuthenticationStatus.Bypass, (await Service().CreateSecondFactorRequestAsync(Context(AllowedIp, config), false)).Code);
        Assert.Equal(AuthenticationStatus.Reject, (await Service().CreateSecondFactorRequestAsync(Context(ForeignIp, config), false)).Code);
    }

    [Fact]
    public async Task IpOnlyIgnoresApiUnreachableGroups()
    {
        var membership = Membership(true);
        var context = Context(ForeignIp, Configuration("IP_ONLY"), Ldap(apiUnreachableGroups: [LegacyGroup]));
        Assert.Equal(AuthenticationStatus.Reject, (await Service(membership: membership).CreateSecondFactorRequestAsync(context, false)).Code);
        membership.Verify(x => x.Execute(It.IsAny<MembershipDto>()), Times.Never);
    }

    [Theory]
    [InlineData(true, AuthenticationStatus.Bypass)]
    [InlineData(false, AuthenticationStatus.Reject)]
    public async Task WithoutPolicyGlobalFlagDecidesWhenNoGroupsAreConfigured(bool bypassWhenUnreachable, AuthenticationStatus expected)
    {
        var config = Configuration(policy: null);
        config.AppSettings.BypassSecondFactorWhenApiUnreachable = bypassWhenUnreachable;
        Assert.Equal(expected, (await Service().CreateSecondFactorRequestAsync(Context(ForeignIp, config), false)).Code);
    }

    [Theory]
    [InlineData(true, AuthenticationStatus.Bypass)]
    [InlineData(false, AuthenticationStatus.Reject)]
    public async Task WithoutPolicyConfiguredGroupsRequireMembership(bool isMember, AuthenticationStatus expected)
    {
        var context = Context(ForeignIp, Configuration(policy: null), Ldap(apiUnreachableGroups: [LegacyGroup]));
        Assert.Equal(expected, (await Service(membership: Membership(isMember)).CreateSecondFactorRequestAsync(context, false)).Code);
    }

    [Fact]
    public async Task WithoutPolicyLdapErrorPropagatesForFailover()
    {
        var context = Context(ForeignIp, Configuration(policy: null), Ldap(apiUnreachableGroups: [LegacyGroup]));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Service(membership: FailingMembership()).CreateSecondFactorRequestAsync(context, false));
    }

    [Theory]
    [InlineData(AdminsGroup, AuthenticationStatus.Bypass)]
    [InlineData(OtherGroup, AuthenticationStatus.Reject)]
    [InlineData(null, AuthenticationStatus.Reject)]
    public async Task GroupOnlyUsesTheSelectedLdapServersGroups(string? group, AuthenticationStatus expected)
    {
        var ldap = group is null ? null : Ldap(loadNestedGroups: false, apiUnreachableGroups: [group]);
        var config = Configuration("GROUP_ONLY");
        config.AppSettings.BypassSecondFactorWhenApiUnreachable = false;
        var context = Context(ForeignIp, config, ldap, memberOf: [AdminsGroup]);
        Assert.Equal(expected, (await Service().CreateSecondFactorRequestAsync(context, false)).Code);
    }

    [Theory]
    [InlineData(RequestStatus.Granted, AuthenticationStatus.Accept)]
    [InlineData(RequestStatus.Denied, AuthenticationStatus.Reject)]
    [InlineData(RequestStatus.AwaitingAuthentication, AuthenticationStatus.Awaiting)]
    public async Task ReachableApiDecidesEvenForWhitelistedClient(RequestStatus status, AuthenticationStatus expected)
    {
        var api = new Mock<ICreateAccessRequest>();
        api.Setup(x => x.Execute(It.IsAny<AccessRequestDto>(), It.IsAny<MultifactorAuthData>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AccessRequestResponse { Status = status });
        var context = Context(AllowedIp, Configuration("OR"), Ldap());
        Assert.Equal(expected, (await Service(api: api, membership: Membership(true)).CreateSecondFactorRequestAsync(context, false)).Code);
    }

    [Fact]
    public async Task OtherApiErrorsDoNotAllowBypass()
    {
        var api = new Mock<ICreateAccessRequest>();
        api.Setup(x => x.Execute(It.IsAny<AccessRequestDto>(), It.IsAny<MultifactorAuthData>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Unexpected failure"));
        var context = Context(AllowedIp, Configuration("IP_ONLY"));
        Assert.Equal(AuthenticationStatus.Reject, (await Service(api: api).CreateSecondFactorRequestAsync(context, false)).Code);
    }

    // --- Source IP ---

    [Theory]
    [InlineData(null, AuthenticationStatus.Bypass)]
    [InlineData("00-11-22-33-44-55", AuthenticationStatus.Bypass)]
    [InlineData(ForeignIp, AuthenticationStatus.Reject)]
    public async Task SourceIpFallsBackToUdpSenderLikeIpWhiteList(string? callingStationId, AuthenticationStatus expected)
    {
        var context = Context(callingStationId, Configuration("IP_ONLY"));
        context.RequestPacket.RemoteEndpoint = new IPEndPoint(IPAddress.Parse(AllowedIp), 1812);
        Assert.Equal(expected, (await Service().CreateSecondFactorRequestAsync(context, false)).Code);
    }

    [Fact]
    public async Task UsesCustomCallingStationIdAttribute()
    {
        var config = Configuration("IP_ONLY");
        config.AppSettings.CallingStationIdAttribute = "Custom-Source-IP";
        var context = Context(ForeignIp, config);
        context.RequestPacket.AddAttributeValue("Custom-Source-IP", AllowedIp);
        Assert.Equal(AuthenticationStatus.Bypass, (await Service().CreateSecondFactorRequestAsync(context, false)).Code);
    }
}
