using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Multifactor.Core.Ldap.Name;
using Multifactor.Radius.Adapter.v2.Application.Core.Enum;
using Multifactor.Radius.Adapter.v2.Application.Core.Models;
using Multifactor.Radius.Adapter.v2.Application.Core.Models.Abstractions;
using Multifactor.Radius.Adapter.v2.Application.Core.Models.Dto;
using Multifactor.Radius.Adapter.v2.Application.Core.Models.Packet;
using Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.SecondFactor.Bypass;
using Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.SecondFactor.Multifactor;
using Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.SecondFactor.Multifactor.Exceptions;
using Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.SecondFactor.Multifactor.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.SecondFactor.Multifactor.Ports;
using Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.SecondFactor.Ports;
using Multifactor.Radius.Adapter.v2.Application.SharedPorts;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configurations.Models;

namespace Multifactor.Radius.Adapter.v2.Tests.Fixtures;

internal static class BypassTestData
{
    public const string IpRules = "192.168.0.1-192.168.0.5; 192.168.0.10; 10.0.0.0/24";
    public const string AllowedIp = "192.168.0.1";
    public const string ForeignIp = "203.0.113.5";
    public const string AdminsGroup = "CN=IT Admins,OU=groups,DC=domain,DC=your";
    public const string DomainAdminsGroup = "CN=Domain Admins,OU=groups,DC=domain,DC=your";
    public const string Groups = AdminsGroup + ";" + DomainAdminsGroup;
    public const string LegacyGroup = "CN=Legacy,DC=domain,DC=your";
    public const string OtherGroup = "CN=Other,DC=domain,DC=your";

    public static AdapterConfiguration Configuration(string? policy, string? ipWhitelist = IpRules) => new()
    {
        FileName = "test",
        AppSettings = new AppSettingsSection
        {
            MultifactorNasIdentifier = "test",
            MultifactorSharedSecret = "test",
            RadiusSharedSecret = "test",
            FirstFactorAuthenticationSource = "none",
            BypassPolicy = policy,
            BypassIpWhitelist = ipWhitelist
        }
    };

    public static ILdapServerConfiguration Ldap(bool loadNestedGroups = true, string[]? apiUnreachableGroups = null)
    {
        var ldap = new Mock<ILdapServerConfiguration>();
        ldap.SetupGet(x => x.ConnectionString).Returns("ldap://domain.your");
        ldap.SetupGet(x => x.LoadNestedGroups).Returns(loadNestedGroups);
        ldap.SetupGet(x => x.NestedGroupsBaseDns).Returns(Array.Empty<DistinguishedName>());
        ldap.SetupGet(x => x.BypassSecondFactorWhenApiUnreachableGroups).Returns(ToDns(apiUnreachableGroups ?? [AdminsGroup, DomainAdminsGroup]));
        return ldap.Object;
    }

    public static Mock<ICheckMembership> Membership(bool isMember)
    {
        var membership = new Mock<ICheckMembership>();
        membership.Setup(x => x.Execute(It.IsAny<MembershipDto>())).Returns(isMember);
        return membership;
    }

    public static Mock<ICheckMembership> FailingMembership()
    {
        var membership = new Mock<ICheckMembership>();
        membership.Setup(x => x.Execute(It.IsAny<MembershipDto>()))
            .Throws(new InvalidOperationException("LDAP server is unavailable"));
        return membership;
    }

    public static RadiusPipelineContext Context(string? callingStationId, AdapterConfiguration config,
        ILdapServerConfiguration? ldap = null, string[]? memberOf = null)
    {
        var packet = new RadiusPacket(RadiusPacketHeader.Create(PacketCode.AccessRequest, 1))
        {
            RemoteEndpoint = new IPEndPoint(IPAddress.Parse("203.0.113.100"), 1812)
        };
        packet.AddAttributeValue("User-Name", "user");
        if (callingStationId != null)
        {
            packet.AddAttributeValue("Calling-Station-Id", callingStationId);
        }

        var profile = new Mock<ILdapProfile>();
        profile.SetupGet(x => x.MemberOf).Returns(ToDns(memberOf));

        return new RadiusPipelineContext(packet, ClientConfiguration.FromConfiguration(config, true), ldap)
        {
            Passphrase = UserPassphrase.Parse(null, PreAuthMode.None),
            LdapProfile = profile.Object
        };
    }

    public static MultifactorApiService Service(Mock<ICreateAccessRequest>? api = null, Mock<ICheckMembership>? membership = null)
    {
        if (api == null)
        {
            api = new Mock<ICreateAccessRequest>();
            api.Setup(x => x.Execute(It.IsAny<AccessRequestDto>(), It.IsAny<MultifactorAuthData>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new MultifactorApiUnreachableException("API unavailable"));
        }

        var challenge = new Mock<ISendChallenge>();
        challenge.Setup(x => x.Execute(It.IsAny<ChallengeRequestDto>(), It.IsAny<MultifactorAuthData>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new MultifactorApiUnreachableException("API unavailable"));

        var bypassPolicy = new ApiUnreachableBypassPolicy(membership?.Object ?? Mock.Of<ICheckMembership>(),
            NullLogger<ApiUnreachableBypassPolicy>.Instance);
        return new MultifactorApiService(Mock.Of<IAuthenticatedClientCache>(), bypassPolicy, api.Object, challenge.Object,
            NullLogger<MultifactorApiService>.Instance);
    }

    private static DistinguishedName[] ToDns(string[]? groups) =>
        (groups ?? []).Select(g => new DistinguishedName(g)).ToArray();
}
