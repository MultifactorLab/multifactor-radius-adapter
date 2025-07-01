using Moq;
using Multifactor.Core.Ldap.Connection;
using Multifactor.Core.Ldap.LdapGroup.Load;
using Multifactor.Core.Ldap.LdapGroup.Membership;
using Multifactor.Core.Ldap.Name;
using Multifactor.Core.Ldap.Schema;
using Multifactor.Radius.Adapter.v2.Core.Ldap;
using Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline.Context;
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
        var ldapConnectionFactoryMock = new Mock<ILdapConnectionFactory>();
        var loaderMock = new Mock<ILdapGroupLoader>();
        var groupsDns = new DistinguishedName[] { new("cn=group1,dc=example,dc=com"), new("cn=group2,dc=example,dc=com"), new("cn=group3,dc=example,dc=com")};
        loaderMock.Setup(x => x.GetGroups(It.IsAny<DistinguishedName>(), It.IsAny<int>())).Returns(groupsDns);
        groupLoaderFactoryMock.Setup(x => x.GetGroupLoader(It.IsAny<ILdapSchema>(), It.IsAny<ILdapConnection>(), It.IsAny<DistinguishedName>())).Returns(loaderMock.Object);
        
        var service = new LdapGroupService(groupLoaderFactoryMock.Object, membershipCheckerFactoryMock.Object, ldapConnectionFactoryMock.Object);
        var actualGroups = service.LoadUserGroups(new LoadUserGroupsRequest(schemaMock.Object, ldapConnectionMock.Object, new DistinguishedName("cn=group1,dc=example,dc=com"), new DistinguishedName("dc=search,dc=base")));

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
        var ldapConnectionFactoryMock = new Mock<ILdapConnectionFactory>();
        var loaderMock = new Mock<ILdapGroupLoader>();
        var groupsDns = new DistinguishedName[] { new("cn=group1,dc=example,dc=com"), new("cn=group2,dc=example,dc=com"), new("cn=group3,dc=example,dc=com")};
        loaderMock.Setup(x => x.GetGroups(It.IsAny<DistinguishedName>(), It.IsAny<int>())).Returns(groupsDns);
        groupLoaderFactoryMock.Setup(x => x.GetGroupLoader(It.IsAny<ILdapSchema>(), It.IsAny<ILdapConnection>(), It.IsAny<DistinguishedName>())).Returns(loaderMock.Object);
        
        var service = new LdapGroupService(groupLoaderFactoryMock.Object, membershipCheckerFactoryMock.Object, ldapConnectionFactoryMock.Object);
        var actualGroups = service.LoadUserGroups(new LoadUserGroupsRequest(schemaMock.Object, ldapConnectionMock.Object, new DistinguishedName("cn=group1,dc=example,dc=com"), new DistinguishedName("dc=search,dc=base"), limit));

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
        var ldapConnectionFactoryMock = new Mock<ILdapConnectionFactory>();
        var loaderMock = new Mock<ILdapGroupLoader>();
        var groupsDns = new DistinguishedName[] { new("cn=group1,dc=example,dc=com"), new("cn=group2,dc=example,dc=com"), new("cn=group3,dc=example,dc=com")};
        loaderMock.Setup(x => x.GetGroups(It.IsAny<DistinguishedName>(), It.IsAny<int>())).Returns(groupsDns);
        groupLoaderFactory.Setup(x => x.GetGroupLoader(It.IsAny<ILdapSchema>(), It.IsAny<ILdapConnection>(), It.IsAny<DistinguishedName>())).Returns(loaderMock.Object);
        
        var service = new LdapGroupService(groupLoaderFactory.Object, membershipCheckerFactoryMock.Object, ldapConnectionFactoryMock.Object);
       Assert.Throws<ArgumentOutOfRangeException>(() => service.LoadUserGroups(new LoadUserGroupsRequest(schemaMock.Object, ldapConnectionMock.Object, new DistinguishedName("cn=group1,dc=example,dc=com"), new DistinguishedName("dc=search,dc=base") ,limit)));
    }
    
    [Fact]
    public void IsMemberOf_LoadNestedGroupsIsFalse_ShouldReturnTrue()
    {
        var groupLoaderFactoryMock = new Mock<ILdapGroupLoaderFactory>();
        var connectionFactory = new Mock<ILdapConnectionFactory>();
        var memberShipCheckerFactoryMock = new Mock<IMembershipCheckerFactory>();
        
        var service = new LdapGroupService(groupLoaderFactoryMock.Object, memberShipCheckerFactoryMock.Object, connectionFactory.Object);

        var contextMock = new Mock<IRadiusPipelineExecutionContext>();
        contextMock.Setup(x => x.UserLdapProfile.Dn).Returns(new DistinguishedName("cn=user,dc=example,dc=com"));
        contextMock.Setup(x => x.UserLdapProfile.MemberOf).Returns([new DistinguishedName("cn=group1,dc=example,dc=com")]);
        contextMock.Setup(x => x.LdapServerConfiguration.LoadNestedGroups).Returns(false);
        contextMock.Setup(x => x.LdapServerConfiguration.NestedGroupsBaseDns).Returns([]);
        contextMock.Setup(x => x.LdapServerConfiguration.ConnectionString).Returns("connectionString");
        contextMock.Setup(x => x.LdapServerConfiguration.UserName).Returns("user");
        contextMock.Setup(x => x.LdapServerConfiguration.Password).Returns("password");
        contextMock.Setup(x => x.LdapServerConfiguration.BindTimeoutInSeconds).Returns(10);
        contextMock.Setup(x => x.LdapSchema).Returns(new Mock<ILdapSchema>().Object);
        contextMock.Setup(x => x.LdapSchema).Returns(new Mock<ILdapSchema>().Object);
        
        var request = new MembershipRequest(contextMock.Object, [new DistinguishedName("cn=group1,dc=example,dc=com")]);

        var isMember = service.IsMemberOf(request);
        Assert.True(isMember);
    }
    
    [Fact]
    public void IsMemberOf_LoadNestedGroupsIsFalseNoMemberOfValues_ShouldReturnFalse()
    {
        var groupLoaderFactoryMock = new Mock<ILdapGroupLoaderFactory>();
        var connectionFactory = new Mock<ILdapConnectionFactory>();
        var memberShipCheckerFactoryMock = new Mock<IMembershipCheckerFactory>();
        
        var service = new LdapGroupService(groupLoaderFactoryMock.Object, memberShipCheckerFactoryMock.Object, connectionFactory.Object);
        var contextMock = new Mock<IRadiusPipelineExecutionContext>();
        contextMock.Setup(x => x.UserLdapProfile.Dn).Returns(new DistinguishedName("cn=user,dc=example,dc=com"));
        contextMock.Setup(x => x.UserLdapProfile.MemberOf).Returns([]);
        contextMock.Setup(x => x.LdapServerConfiguration.LoadNestedGroups).Returns(false);
        contextMock.Setup(x => x.LdapServerConfiguration.NestedGroupsBaseDns).Returns([]);
        contextMock.Setup(x => x.LdapServerConfiguration.ConnectionString).Returns("connectionString");
        contextMock.Setup(x => x.LdapServerConfiguration.UserName).Returns("user");
        contextMock.Setup(x => x.LdapServerConfiguration.Password).Returns("password");
        contextMock.Setup(x => x.LdapServerConfiguration.BindTimeoutInSeconds).Returns(10);
        contextMock.Setup(x => x.LdapSchema).Returns(new Mock<ILdapSchema>().Object);
        contextMock.Setup(x => x.LdapSchema).Returns(new Mock<ILdapSchema>().Object);
        
        var request = new MembershipRequest(contextMock.Object, [new DistinguishedName("cn=group1,dc=example,dc=com")]);

        var isMember = service.IsMemberOf(request);
        Assert.False(isMember);
    }
    
    [Fact]
    public void IsMemberOf_LoadNestedGroupsIsFalse_ShouldReturnFalse()
    {
        var groupLoaderFactoryMock = new Mock<ILdapGroupLoaderFactory>();
        var connectionFactory = new Mock<ILdapConnectionFactory>();
        var memberShipCheckerFactoryMock = new Mock<IMembershipCheckerFactory>();
        
        var service = new LdapGroupService(groupLoaderFactoryMock.Object, memberShipCheckerFactoryMock.Object, connectionFactory.Object);
        
        var contextMock = new Mock<IRadiusPipelineExecutionContext>();
        contextMock.Setup(x => x.UserLdapProfile.Dn).Returns(new DistinguishedName("cn=user,dc=example,dc=com"));
        contextMock.Setup(x => x.UserLdapProfile.MemberOf).Returns([new DistinguishedName("cn=group1,dc=example,dc=com")]);
        contextMock.Setup(x => x.LdapServerConfiguration.LoadNestedGroups).Returns(false);
        contextMock.Setup(x => x.LdapServerConfiguration.NestedGroupsBaseDns).Returns([]);
        contextMock.Setup(x => x.LdapServerConfiguration.ConnectionString).Returns("connectionString");
        contextMock.Setup(x => x.LdapServerConfiguration.UserName).Returns("user");
        contextMock.Setup(x => x.LdapServerConfiguration.Password).Returns("password");
        contextMock.Setup(x => x.LdapServerConfiguration.BindTimeoutInSeconds).Returns(10);
        contextMock.Setup(x => x.LdapSchema).Returns(new Mock<ILdapSchema>().Object);
        
        var request = new MembershipRequest(contextMock.Object, [new DistinguishedName("cn=group2,dc=example,dc=com")]);

        var isMember = service.IsMemberOf(request);
        Assert.False(isMember);
    }
    
    [Fact]
    public void IsMemberOf_LoadNestedGroupsIsTrueNoBaseDns_ShouldReturnTrue()
    {
        var groupLoaderFactoryMock = new Mock<ILdapGroupLoaderFactory>();
        var connectionFactory = new Mock<ILdapConnectionFactory>();
        connectionFactory.Setup(x=> x.CreateConnection(It.IsAny<LdapConnectionOptions>())).Returns(new Mock<ILdapConnection>().Object);
        
        var memberShipCheckerFactoryMock = new Mock<IMembershipCheckerFactory>();
        var checkerMock = new Mock<IMembershipChecker>();
        var namingContext = new DistinguishedName("dc=example,dc=com");
        checkerMock.Setup(x=> x.IsMemberOf(It.IsAny<DistinguishedName>(), It.IsAny<DistinguishedName>())).Returns(true);
        memberShipCheckerFactoryMock
            .Setup(x => x.GetMembershipChecker(It.IsAny<ILdapSchema>(), It.IsAny<ILdapConnection>(), namingContext))
            .Returns(checkerMock.Object);
        
        var service = new LdapGroupService(groupLoaderFactoryMock.Object, memberShipCheckerFactoryMock.Object, connectionFactory.Object);
        var schemaMock = new Mock<ILdapSchema>();
        schemaMock.Setup(x => x.NamingContext).Returns(namingContext);
        
        var contextMock = new Mock<IRadiusPipelineExecutionContext>();
        contextMock.Setup(x => x.UserLdapProfile.Dn).Returns(new DistinguishedName("cn=user,dc=example,dc=com"));
        contextMock.Setup(x => x.UserLdapProfile.MemberOf).Returns([new DistinguishedName("cn=group1,dc=example,dc=com")]);
        contextMock.Setup(x => x.LdapServerConfiguration.LoadNestedGroups).Returns(true);
        contextMock.Setup(x => x.LdapServerConfiguration.NestedGroupsBaseDns).Returns([]);
        contextMock.Setup(x => x.LdapServerConfiguration.ConnectionString).Returns("connectionString");
        contextMock.Setup(x => x.LdapServerConfiguration.UserName).Returns("user");
        contextMock.Setup(x => x.LdapServerConfiguration.Password).Returns("password");
        contextMock.Setup(x => x.LdapServerConfiguration.BindTimeoutInSeconds).Returns(10);
        contextMock.Setup(x => x.LdapSchema).Returns(schemaMock.Object);
        
        var request = new MembershipRequest(contextMock.Object, [new DistinguishedName("cn=group2,dc=example,dc=com")]);

        var isMember = service.IsMemberOf(request);
        Assert.True(isMember);
    }
    
    [Fact]
    public void IsMemberOf_LoadNestedGroupsIsTrueHasBaseDns_ShouldReturnTrue()
    {
        var groupLoaderFactoryMock = new Mock<ILdapGroupLoaderFactory>();
        var connectionFactory = new Mock<ILdapConnectionFactory>();
        connectionFactory.Setup(x=> x.CreateConnection(It.IsAny<LdapConnectionOptions>())).Returns(new Mock<ILdapConnection>().Object);
        
        var memberShipCheckerFactoryMock = new Mock<IMembershipCheckerFactory>();
        var checkerMock = new Mock<IMembershipChecker>();
        var namingContext = new DistinguishedName("dc=example1,dc=com");
        checkerMock.Setup(x=> x.IsMemberOf(It.IsAny<DistinguishedName>(), It.IsAny<DistinguishedName>())).Returns(true);
        memberShipCheckerFactoryMock
            .Setup(x => x.GetMembershipChecker(It.IsAny<ILdapSchema>(), It.IsAny<ILdapConnection>(), namingContext))
            .Returns(checkerMock.Object);
        
        var service = new LdapGroupService(groupLoaderFactoryMock.Object, memberShipCheckerFactoryMock.Object, connectionFactory.Object);
        var contextMock = new Mock<IRadiusPipelineExecutionContext>();
        contextMock.Setup(x => x.UserLdapProfile.Dn).Returns(new DistinguishedName("cn=user,dc=example,dc=com"));
        contextMock.Setup(x => x.UserLdapProfile.MemberOf).Returns([new DistinguishedName("cn=group1,dc=example,dc=com")]);
        contextMock.Setup(x => x.LdapServerConfiguration.LoadNestedGroups).Returns(true);
        contextMock.Setup(x => x.LdapServerConfiguration.NestedGroupsBaseDns).Returns(["dc=example1,dc=com"]);
        contextMock.Setup(x => x.LdapServerConfiguration.ConnectionString).Returns("connectionString");
        contextMock.Setup(x => x.LdapServerConfiguration.UserName).Returns("user");
        contextMock.Setup(x => x.LdapServerConfiguration.Password).Returns("password");
        contextMock.Setup(x => x.LdapServerConfiguration.BindTimeoutInSeconds).Returns(10);
        contextMock.Setup(x => x.LdapSchema).Returns(new Mock<ILdapSchema>().Object);
        
        var request = new MembershipRequest(contextMock.Object, [new DistinguishedName("cn=group2,dc=example,dc=com")]);

        var isMember = service.IsMemberOf(request);
        Assert.True(isMember);
    }
    
    [Fact]
    public void IsMemberOf_LoadNestedGroupsIsTrueNoBaseDns_ShouldReturnFalse()
    {
        var groupLoaderFactoryMock = new Mock<ILdapGroupLoaderFactory>();
        var connectionFactory = new Mock<ILdapConnectionFactory>();
        connectionFactory.Setup(x=> x.CreateConnection(It.IsAny<LdapConnectionOptions>())).Returns(new Mock<ILdapConnection>().Object);
        
        var memberShipCheckerFactoryMock = new Mock<IMembershipCheckerFactory>();
        var checkerMock = new Mock<IMembershipChecker>();
        var namingContext = new DistinguishedName("dc=example,dc=com");
        checkerMock.Setup(x=> x.IsMemberOf(It.IsAny<DistinguishedName>(), It.IsAny<DistinguishedName>())).Returns(false);
        memberShipCheckerFactoryMock
            .Setup(x => x.GetMembershipChecker(It.IsAny<ILdapSchema>(), It.IsAny<ILdapConnection>(), namingContext))
            .Returns(checkerMock.Object);
        
        var service = new LdapGroupService(groupLoaderFactoryMock.Object, memberShipCheckerFactoryMock.Object, connectionFactory.Object);
        var schemaMock = new Mock<ILdapSchema>();
        schemaMock.Setup(x => x.NamingContext).Returns(namingContext);
        var contextMock = new Mock<IRadiusPipelineExecutionContext>();
        contextMock.Setup(x => x.UserLdapProfile.Dn).Returns(new DistinguishedName("cn=user,dc=example,dc=com"));
        contextMock.Setup(x => x.UserLdapProfile.MemberOf).Returns([new DistinguishedName("cn=group1,dc=example,dc=com")]);
        contextMock.Setup(x => x.LdapServerConfiguration.LoadNestedGroups).Returns(true);
        contextMock.Setup(x => x.LdapServerConfiguration.NestedGroupsBaseDns).Returns([]);
        contextMock.Setup(x => x.LdapServerConfiguration.ConnectionString).Returns("connectionString");
        contextMock.Setup(x => x.LdapServerConfiguration.UserName).Returns("user");
        contextMock.Setup(x => x.LdapServerConfiguration.Password).Returns("password");
        contextMock.Setup(x => x.LdapServerConfiguration.BindTimeoutInSeconds).Returns(10);
        contextMock.Setup(x => x.LdapSchema).Returns(schemaMock.Object);
        var request = new MembershipRequest(contextMock.Object, [new DistinguishedName("cn=group2,dc=example,dc=com")]);

        var isMember = service.IsMemberOf(request);
        Assert.False(isMember);
    }
    
    [Fact]
    public void IsMemberOf_LoadNestedGroupsIsTrueHasBaseDns_ShouldReturnFalse()
    {
        var groupLoaderFactoryMock = new Mock<ILdapGroupLoaderFactory>();
        var connectionFactory = new Mock<ILdapConnectionFactory>();
        connectionFactory.Setup(x=> x.CreateConnection(It.IsAny<LdapConnectionOptions>())).Returns(new Mock<ILdapConnection>().Object);
        
        var memberShipCheckerFactoryMock = new Mock<IMembershipCheckerFactory>();
        var checkerMock = new Mock<IMembershipChecker>();
        var namingContext = new DistinguishedName("dc=example1,dc=com");
        checkerMock.Setup(x=> x.IsMemberOf(It.IsAny<DistinguishedName>(), It.IsAny<DistinguishedName>())).Returns(false);
        memberShipCheckerFactoryMock
            .Setup(x => x.GetMembershipChecker(It.IsAny<ILdapSchema>(), It.IsAny<ILdapConnection>(), namingContext))
            .Returns(checkerMock.Object);
        
        var service = new LdapGroupService(groupLoaderFactoryMock.Object, memberShipCheckerFactoryMock.Object, connectionFactory.Object);
        var contextMock = new Mock<IRadiusPipelineExecutionContext>();
        contextMock.Setup(x => x.UserLdapProfile.Dn).Returns(new DistinguishedName("cn=user,dc=example,dc=com"));
        contextMock.Setup(x => x.UserLdapProfile.MemberOf).Returns([new DistinguishedName("cn=group1,dc=example,dc=com")]);
        contextMock.Setup(x => x.LdapServerConfiguration.LoadNestedGroups).Returns(true);
        contextMock.Setup(x => x.LdapServerConfiguration.NestedGroupsBaseDns).Returns(["dc=example1,dc=com"]);
        contextMock.Setup(x => x.LdapServerConfiguration.ConnectionString).Returns("connectionString");
        contextMock.Setup(x => x.LdapServerConfiguration.UserName).Returns("user");
        contextMock.Setup(x => x.LdapServerConfiguration.Password).Returns("password");
        contextMock.Setup(x => x.LdapServerConfiguration.BindTimeoutInSeconds).Returns(10);
        contextMock.Setup(x => x.LdapSchema).Returns(new Mock<ILdapSchema>().Object);
        var request = new MembershipRequest(contextMock.Object, [new DistinguishedName("cn=group2,dc=example,dc=com")]);

        var isMember = service.IsMemberOf(request);
        Assert.False(isMember);
    }
}