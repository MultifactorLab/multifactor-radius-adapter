using Moq;
using Multifactor.Core.Ldap.Connection;
using Multifactor.Core.Ldap.LdapGroup.Load;
using Multifactor.Core.Ldap.LdapGroup.Membership;
using Multifactor.Core.Ldap.Name;
using Multifactor.Core.Ldap.Schema;
using Multifactor.Radius.Adapter.v2.Services.Ldap;

namespace Multifactor.Radius.Adapter.v2.Tests.Unit;

public class LdapGroupServiceTests
{
    [Fact]
    public void LoadUserGroup_ShouldLoadAllGroups()
    {
        var schemaMock = new Mock<ILdapSchema>();
        var ldapConnectionMock = new Mock<ILdapConnection>();
        var groupLoaderFactoryMock = new Mock<ILdapGroupLoaderFactory>();
        var membershipCheckerFactoryMock = new Mock<IMembershipCheckerFactory>();
        var loaderMock = new Mock<ILdapGroupLoader>();
        var groupsDns = new DistinguishedName[] { new("cn=group1,dc=example,dc=com"), new("cn=group2,dc=example,dc=com"), new("cn=group3,dc=example,dc=com")};
        loaderMock.Setup(x => x.GetGroups(It.IsAny<DistinguishedName>(), It.IsAny<int>())).Returns(groupsDns);
        groupLoaderFactoryMock.Setup(x => x.GetGroupLoader(It.IsAny<ILdapSchema>(), It.IsAny<ILdapConnection>(), It.IsAny<DistinguishedName>())).Returns(loaderMock.Object);
        
        var service = new LdapGroupService(groupLoaderFactoryMock.Object, membershipCheckerFactoryMock.Object);
        var actualGroups = service.LoadUserGroups(schemaMock.Object, ldapConnectionMock.Object, new DistinguishedName("cn=group1,dc=example,dc=com"), new DistinguishedName("dc=search,dc=base"));

        var expectedGroups = new[] { "group1", "group2", "group3" };
        Assert.Equal(expectedGroups.Length, actualGroups.Count);
        Assert.True(expectedGroups.SequenceEqual(actualGroups));
    }
    
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void LoadUserGroup_ShouldLoadLimitedNumberOfGroups(int limit)
    {
        var schemaMock = new Mock<ILdapSchema>();
        var ldapConnectionMock = new Mock<ILdapConnection>();
        var membershipCheckerFactoryMock = new Mock<IMembershipCheckerFactory>();
        var groupLoaderFactoryMock = new Mock<ILdapGroupLoaderFactory>();
        var loaderMock = new Mock<ILdapGroupLoader>();
        var groupsDns = new DistinguishedName[] { new("cn=group1,dc=example,dc=com"), new("cn=group2,dc=example,dc=com"), new("cn=group3,dc=example,dc=com")};
        loaderMock.Setup(x => x.GetGroups(It.IsAny<DistinguishedName>(), It.IsAny<int>())).Returns(groupsDns);
        groupLoaderFactoryMock.Setup(x => x.GetGroupLoader(It.IsAny<ILdapSchema>(), It.IsAny<ILdapConnection>(), It.IsAny<DistinguishedName>())).Returns(loaderMock.Object);
        
        var service = new LdapGroupService(groupLoaderFactoryMock.Object, membershipCheckerFactoryMock.Object);
        var actualGroups = service.LoadUserGroups(schemaMock.Object, ldapConnectionMock.Object, new DistinguishedName("cn=group1,dc=example,dc=com"), new DistinguishedName("dc=search,dc=base"), limit);

        var expectedGroups = new string[] { "group1", "group2", "group3" };
        Assert.Equal(limit, actualGroups.Count);
        Assert.True(expectedGroups.Take(limit).SequenceEqual(actualGroups));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void LoadUserGroup_InvalidLimit_ShouldThrowException(int limit)
    {
        var schemaMock = new Mock<ILdapSchema>();
        var ldapConnectionMock = new Mock<ILdapConnection>();
        var membershipCheckerFactoryMock = new Mock<IMembershipCheckerFactory>();
        var groupLoaderFactory = new Mock<ILdapGroupLoaderFactory>();
        var loaderMock = new Mock<ILdapGroupLoader>();
        var groupsDns = new DistinguishedName[] { new("cn=group1,dc=example,dc=com"), new("cn=group2,dc=example,dc=com"), new("cn=group3,dc=example,dc=com")};
        loaderMock.Setup(x => x.GetGroups(It.IsAny<DistinguishedName>(), It.IsAny<int>())).Returns(groupsDns);
        groupLoaderFactory.Setup(x => x.GetGroupLoader(It.IsAny<ILdapSchema>(), It.IsAny<ILdapConnection>(), It.IsAny<DistinguishedName>())).Returns(loaderMock.Object);
        
        var service = new LdapGroupService(groupLoaderFactory.Object, membershipCheckerFactoryMock.Object);
       Assert.Throws<ArgumentOutOfRangeException>(() => service.LoadUserGroups(schemaMock.Object, ldapConnectionMock.Object, new DistinguishedName("cn=group1,dc=example,dc=com"), new DistinguishedName("dc=search,dc=base") ,limit));
    }
    
    [Fact]
    public void LoadUserGroup_UserNameIsNull_ShouldThrowException()
    {
        var schemaMock = new Mock<ILdapSchema>();
        var ldapConnectionMock = new Mock<ILdapConnection>();
        var groupLoaderFactory = new Mock<ILdapGroupLoaderFactory>();
        var membershipCheckerFactoryMock = new Mock<IMembershipCheckerFactory>();
        var loaderMock = new Mock<ILdapGroupLoader>();
        var groupsDns = new DistinguishedName[] { new("cn=group1,dc=example,dc=com"), new("cn=group2,dc=example,dc=com"), new("cn=group3,dc=example,dc=com")};
        loaderMock.Setup(x => x.GetGroups(It.IsAny<DistinguishedName>(), It.IsAny<int>())).Returns(groupsDns);
        groupLoaderFactory.Setup(x => x.GetGroupLoader(It.IsAny<ILdapSchema>(), It.IsAny<ILdapConnection>(), It.IsAny<DistinguishedName>())).Returns(loaderMock.Object);
        
        var service = new LdapGroupService( groupLoaderFactory.Object, membershipCheckerFactoryMock.Object);
        Assert.ThrowsAny<ArgumentException>(() => service.LoadUserGroups(schemaMock.Object, ldapConnectionMock.Object, null!));
    }
}