using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Multifactor.Core.Ldap.Attributes;
using Multifactor.Core.Ldap.Name;
using Multifactor.Core.Ldap.Schema;
using Multifactor.Radius.Adapter.v2.Core.Configuration.Client;
using Multifactor.Radius.Adapter.v2.Core.Ldap;
using Multifactor.Radius.Adapter.v2.Core.Ldap.Identity;
using Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline.Context;
using Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline.Steps;
using Multifactor.Radius.Adapter.v2.Services.Ldap;
using Multifactor.Radius.Adapter.v2.Tests.Fixture;

namespace Multifactor.Radius.Adapter.v2.Tests.PipelineTests.StepsTests;

public class ProfileLoadingTests
{
    [Fact]
    public async Task ExecStep_ShouldLoadProfile()
    {
        var loaderMock = new Mock<ILdapProfileService>();
        var profile = new LdapProfileMock();
        loaderMock
            .Setup(x => x.LoadLdapProfile(It.IsAny<string>(), It.IsAny<ILdapServerConfiguration>(), It.IsAny<DistinguishedName>(), It.IsAny<UserIdentity>(), It.IsAny<LdapAttributeName[]>()))
            .Returns(profile);
        var schemaMock = new Mock<ILdapSchema>();
        schemaMock.Setup(x => x.NamingContext).Returns(new DistinguishedName("dc=test,dc=example,dc=com"));
        var contextMock = new Mock<IRadiusPipelineExecutionContext>();
        contextMock.Setup(x => x.RequestPacket.UserName).Returns("user@example.com");
        contextMock.Setup(x => x.Settings.RadiusReplyAttributes).Returns(new Dictionary<string, RadiusReplyAttributeValue[]>());
        contextMock.Setup(x => x.LdapSchema).Returns(schemaMock.Object);
        contextMock.SetupProperty(x => x.UserLdapProfile);
        var context = contextMock.Object;
        
        var step = new ProfileLoadingStep(loaderMock.Object, NullLogger<ProfileLoadingStep>.Instance);
        await step.ExecuteAsync(context);

        Assert.NotNull(context.UserLdapProfile);
        Assert.Equal(profile.Dn, context.UserLdapProfile.Dn);
    }
    
    [Theory]
    [ClassData(typeof(EmptyStringsListInput))]
    public async Task ExecStep_NoUserName_ShouldDoNothing(string userName)
    {
        var loaderMock = new Mock<ILdapProfileService>();
        var profile = new LdapProfileMock();
        loaderMock
            .Setup(x => x.LoadLdapProfile(It.IsAny<string>(), It.IsAny<ILdapServerConfiguration>(), It.IsAny<DistinguishedName>(), It.IsAny<UserIdentity>(), It.IsAny<LdapAttributeName[]>()))
            .Returns(profile);
        
        var contextMock = new Mock<IRadiusPipelineExecutionContext>();
        contextMock.Setup(x => x.RequestPacket.UserName).Returns(userName);
        contextMock.Setup(x => x.RemoteEndpoint).Returns(IPEndPoint.Parse("127.0.0.1:666"));
        contextMock.SetupProperty(x => x.UserLdapProfile);
        var context = contextMock.Object;
        
        var step = new ProfileLoadingStep(loaderMock.Object, NullLogger<ProfileLoadingStep>.Instance);
        await step.ExecuteAsync(context);

        Assert.Null(context.UserLdapProfile);
    }
    
    [Fact]
    public async Task ExecStep_NoLdapProfile_ShouldDoNothing()
    {
        var loaderMock = new Mock<ILdapProfileService>();
        loaderMock
            .Setup(x => x.LoadLdapProfile(It.IsAny<string>(), It.IsAny<ILdapServerConfiguration>(), It.IsAny<DistinguishedName>(), It.IsAny<UserIdentity>(), It.IsAny<LdapAttributeName[]>()))
            .Returns(() => null);
        var schemaMock = new Mock<ILdapSchema>();
        schemaMock.Setup(x => x.NamingContext).Returns(new DistinguishedName("dc=test,dc=example,dc=com"));
        var contextMock = new Mock<IRadiusPipelineExecutionContext>();
        contextMock.Setup(x => x.RequestPacket.UserName).Returns("user@example.com");
        contextMock.Setup(x => x.Settings.RadiusReplyAttributes).Returns(new Dictionary<string, RadiusReplyAttributeValue[]>());
        contextMock.Setup(x => x.LdapSchema).Returns(schemaMock.Object);
        contextMock.SetupProperty(x => x.UserLdapProfile);
        var context = contextMock.Object;
        
        var step = new ProfileLoadingStep(loaderMock.Object, NullLogger<ProfileLoadingStep>.Instance);
        await step.ExecuteAsync(context);

        Assert.Null(context.UserLdapProfile);
    }
    
    [Fact]
    public async Task ExecStep_NoLdapSchema_ShouldDoNothing()
    {
        var loaderMock = new Mock<ILdapProfileService>();
        loaderMock
            .Setup(x => x.LoadLdapProfile(It.IsAny<string>(), It.IsAny<ILdapServerConfiguration>(), It.IsAny<DistinguishedName>(), It.IsAny<UserIdentity>(), It.IsAny<LdapAttributeName[]>()))
            .Returns(() => null);
        var contextMock = new Mock<IRadiusPipelineExecutionContext>();
        contextMock.Setup(x => x.RequestPacket.UserName).Returns("user@example.com");
        contextMock.Setup(x => x.Settings.RadiusReplyAttributes).Returns(new Dictionary<string, RadiusReplyAttributeValue[]>());
        contextMock.Setup(x => x.LdapSchema).Returns(() => null);
        contextMock.SetupProperty(x => x.UserLdapProfile);
        var context = contextMock.Object;
        
        var step = new ProfileLoadingStep(loaderMock.Object, NullLogger<ProfileLoadingStep>.Instance);
        await step.ExecuteAsync(context);

        Assert.Null(context.UserLdapProfile);
    }

    private class LdapProfileMock : ILdapProfile
    {
        public DistinguishedName Dn { get; }
        public string? Upn { get; }
        public string? Phone { get; }
        public string? Email { get; }
        public string? DisplayName { get; }
        public IReadOnlyCollection<DistinguishedName> MemberOf { get; }
        public IReadOnlyCollection<LdapAttribute> Attributes { get; }

        public LdapProfileMock()
        {
            Dn = new DistinguishedName("dc=test,dc=example,dc=com");
        }
    }
}