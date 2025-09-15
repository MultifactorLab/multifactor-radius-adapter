using System.DirectoryServices.Protocols;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Multifactor.Core.Ldap;
using Multifactor.Core.Ldap.Connection;
using Multifactor.Core.Ldap.Name;
using Multifactor.Core.Ldap.Schema;
using Multifactor.Radius.Adapter.v2.Core.Ldap;
using Multifactor.Radius.Adapter.v2.Core.Ldap.Forest;
using Multifactor.Radius.Adapter.v2.Services.Cache;
using Multifactor.Radius.Adapter.v2.Services.Ldap;
using Multifactor.Radius.Adapter.v2.Services.Ldap.Forest;
using ILdapConnection = Multifactor.Radius.Adapter.v2.Core.Ldap.ILdapConnection;

namespace Multifactor.Radius.Adapter.v2.Tests.Unit;

public class LdapForestServiceTests
{
    [Fact]
    public void LoadLdapForest_EmptyMainSchema_ShouldReturnEmptyForest()
    {
        //Arrange
        var ldapSchemaLoaderMock = new Mock<ILdapSchemaLoader>();
        ldapSchemaLoaderMock.Setup(x => x.Load(It.IsAny<LdapConnectionOptions>())).Returns(() => null);
        var connectionFactoryMock = new Mock<ILdapConnectionFactory>();
        var domainsLoaderProviderMock = new Mock<ILdapForestLoaderProvider>();
        var cacheMock = new Mock<ICacheService>();
        var forestService = new LdapForestService(
            ldapSchemaLoaderMock.Object,
            connectionFactoryMock.Object,
            domainsLoaderProviderMock.Object,
            cacheMock.Object,
            NullLogger<LdapForestService>.Instance);
        var options = GetConnectionOptions();
        
        //Act
        var result = forestService.LoadLdapForest(options, true, true);
        
        //Assert
        Assert.Empty(result);
    }
    
    [Theory]
    [InlineData(LdapImplementation.OpenLDAP)]
    [InlineData(LdapImplementation.Samba)]
    [InlineData(LdapImplementation.Unknown)]
    [InlineData(LdapImplementation.ActiveDirectory)]
    [InlineData(LdapImplementation.MultiDirectory)]
    [InlineData(LdapImplementation.FreeIPA)]
    public void LoadLdapForest_NoTrustedDomainsLoader_ShouldReturnMainSchema(LdapImplementation ldapImplementation)
    {
        //Arrange
        var ldapSchemaLoaderMock = new Mock<ILdapSchemaLoader>();
        var ldapSchemaMock = new Mock<ILdapSchema>();
        var namingContext = new DistinguishedName("dc=domain,dc=com");
        ldapSchemaMock.Setup(x => x.LdapServerImplementation).Returns(ldapImplementation);
        ldapSchemaMock.Setup(x => x.NamingContext).Returns(namingContext);
        
        ldapSchemaLoaderMock.Setup(x => x.Load(It.IsAny<LdapConnectionOptions>())).Returns(ldapSchemaMock.Object);
        var connectionFactoryMock = new Mock<ILdapConnectionFactory>();
        var domainsLoaderProviderMock = new Mock<ILdapForestLoaderProvider>();
        domainsLoaderProviderMock.Setup(x => x.GetTrustedDomainsLoader(ldapImplementation)).Returns(() => null);
        var cacheMock = new Mock<ICacheService>();
        
        var forestService = new LdapForestService(
            ldapSchemaLoaderMock.Object,
            connectionFactoryMock.Object,
            domainsLoaderProviderMock.Object,
            cacheMock.Object,
            NullLogger<LdapForestService>.Instance);
        var options = GetConnectionOptions();
        
        //Act
        var result = forestService.LoadLdapForest(options, true, true);

        //Assert
        Assert.NotNull(result);
        Assert.Single(result);
        var tree = result.First();
        Assert.Equal(ldapSchemaMock.Object, tree.Schema);
    }
    
    [Fact]
    public void LoadLdapForest_LoadTrustedDomainsFalse_ShouldReturnMainSchema()
    {
        //Arrange
        var ldapSchemaLoaderMock = new Mock<ILdapSchemaLoader>();
        var ldapSchemaMock = new Mock<ILdapSchema>();
        var namingContext = new DistinguishedName("dc=domain,dc=com");
        ldapSchemaMock.Setup(x => x.LdapServerImplementation).Returns(LdapImplementation.Unknown);
        ldapSchemaMock.Setup(x => x.NamingContext).Returns(namingContext);
        ldapSchemaLoaderMock.Setup(x => x.Load(It.IsAny<LdapConnectionOptions>())).Returns(ldapSchemaMock.Object);
        
        var connectionFactoryMock = new Mock<ILdapConnectionFactory>();
        var domainLoaderMock = new Mock<ILdapForestLoader>();
        var domainsLoaderProviderMock = new Mock<ILdapForestLoaderProvider>();
        domainsLoaderProviderMock.Setup(x => x.GetTrustedDomainsLoader(It.IsAny<LdapImplementation>())).Returns(() => domainLoaderMock.Object);
        var cacheMock = new Mock<ICacheService>();
            
        var forestService = new LdapForestService(
            ldapSchemaLoaderMock.Object,
            connectionFactoryMock.Object,
            domainsLoaderProviderMock.Object,
            cacheMock.Object,
            NullLogger<LdapForestService>.Instance);
        var options = GetConnectionOptions();
        
        //Act
        var result = forestService.LoadLdapForest(options, false, false);

        //Assert
        Assert.NotNull(result);
        Assert.Single(result);
        var tree = result.First();
        Assert.Equal(ldapSchemaMock.Object, tree.Schema);
        domainLoaderMock.Verify(x => x.LoadTrustedDomains(It.IsAny<ILdapConnection>(), It.IsAny<ILdapSchema>()), Times.Never());
        ldapSchemaLoaderMock.Verify(x => x.Load(It.IsAny<LdapConnectionOptions>()), Times.Once);
    }
    
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void LoadLdapForest_LoadTrustedDomainsTrue_ShouldReturnMainSchemaAndTrustedDomain(int trustedDomainsCount)
    {
        //Arrange
        var options = GetConnectionOptions();
        var ldapSchemaLoaderMock = new Mock<ILdapSchemaLoader>();
        var rootSchemaMock = new Mock<ILdapSchema>();
        
        var namingContext = new DistinguishedName("dc=domain,dc=com");
        rootSchemaMock.Setup(x => x.LdapServerImplementation).Returns(LdapImplementation.ActiveDirectory);
        rootSchemaMock.Setup(x => x.NamingContext).Returns(namingContext);
        ldapSchemaLoaderMock.Setup(x => x.Load(It.Is<LdapConnectionOptions>(c => c.ConnectionString == options.ConnectionString))).Returns(rootSchemaMock.Object);
        
        var trustedSchemaMock = new Mock<ILdapSchema>();
        trustedSchemaMock.Setup(x => x.LdapServerImplementation).Returns(LdapImplementation.Unknown);
        trustedSchemaMock.Setup(x => x.NamingContext).Returns(new DistinguishedName("dc=trusted,dc=domain"));
        ldapSchemaLoaderMock.Setup(x => x.Load(It.Is<LdapConnectionOptions>(c => c.ConnectionString != options.ConnectionString))).Returns(trustedSchemaMock.Object);
        
        var connectionFactoryMock = new Mock<ILdapConnectionFactory>();
        var domainLoaderMock = new Mock<ILdapForestLoader>();
        var trustedDomains = Enumerable.Repeat(new DistinguishedName("dc=trusted,dc=domain"), trustedDomainsCount);
        
        domainLoaderMock.Setup(x=> x.LoadTrustedDomains(It.IsAny<ILdapConnection>(), rootSchemaMock.Object)).Returns(trustedDomains);
        
        var domainsLoaderProviderMock = new Mock<ILdapForestLoaderProvider>();
        domainsLoaderProviderMock.Setup(x => x.GetTrustedDomainsLoader(LdapImplementation.ActiveDirectory)).Returns(() => domainLoaderMock.Object);
        var cacheMock = new Mock<ICacheService>();
        
        var forestService = new LdapForestService(
            ldapSchemaLoaderMock.Object,
            connectionFactoryMock.Object,
            domainsLoaderProviderMock.Object,
            cacheMock.Object,
            NullLogger<LdapForestService>.Instance);
        
        //Act
        var result = forestService.LoadLdapForest(options, true, false);

        //Assert
        var domainsCount = trustedDomainsCount + 1;
        Assert.NotNull(result);
        Assert.Equal(domainsCount, result.Count);
        domainLoaderMock.Verify(x => x.LoadTrustedDomains(It.IsAny<ILdapConnection>(), It.IsAny<ILdapSchema>()), Times.Once());
        ldapSchemaLoaderMock.Verify(x => x.Load(It.IsAny<LdapConnectionOptions>()), Times.Exactly(domainsCount));
    }
    
    [Fact]
    public void LoadLdapForest_LoadSuffixesFalse_ShouldReturnMainSuffix()
    {
        //Arrange
        var ldapSchemaLoaderMock = new Mock<ILdapSchemaLoader>();
        var ldapSchemaMock = new Mock<ILdapSchema>();
        var namingContext = new DistinguishedName("dc=domain,dc=com");
        ldapSchemaMock.Setup(x => x.LdapServerImplementation).Returns(LdapImplementation.Unknown);
        ldapSchemaMock.Setup(x => x.NamingContext).Returns(namingContext);
        ldapSchemaLoaderMock.Setup(x => x.Load(It.IsAny<LdapConnectionOptions>())).Returns(ldapSchemaMock.Object);
        
        var connectionFactoryMock = new Mock<ILdapConnectionFactory>();
        var domainLoaderMock = new Mock<ILdapForestLoader>();
        var domainsLoaderProviderMock = new Mock<ILdapForestLoaderProvider>();
        domainsLoaderProviderMock.Setup(x => x.GetTrustedDomainsLoader(It.IsAny<LdapImplementation>())).Returns(() => domainLoaderMock.Object);
        var cacheMock = new Mock<ICacheService>();
        
        var forestService = new LdapForestService(
            ldapSchemaLoaderMock.Object,
            connectionFactoryMock.Object,
            domainsLoaderProviderMock.Object,
            cacheMock.Object,
            NullLogger<LdapForestService>.Instance);
        var options = GetConnectionOptions();
        
        //Act
        var result = forestService.LoadLdapForest(options, false, false);

        //Assert
        Assert.NotNull(result);
        Assert.Single(result);
        var tree = result.First();
        Assert.Equal(ldapSchemaMock.Object, tree.Schema);
        Assert.Single(tree.Suffixes);
        domainLoaderMock.Verify(x => x.LoadDomainSuffixes(It.IsAny<ILdapConnection>(), It.IsAny<ILdapSchema>()), Times.Never());
    }
    
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void LoadLdapForest_LoadSuffixesTrue_ShouldReturnAllSuffixes(int suffixCount)
    {
        //Arrange
        var ldapSchemaLoaderMock = new Mock<ILdapSchemaLoader>();
        var ldapSchemaMock = new Mock<ILdapSchema>();
        var namingContext = new DistinguishedName("dc=domain,dc=com");
        ldapSchemaMock.Setup(x => x.LdapServerImplementation).Returns(LdapImplementation.Unknown);
        ldapSchemaMock.Setup(x => x.NamingContext).Returns(namingContext);
        ldapSchemaLoaderMock.Setup(x => x.Load(It.IsAny<LdapConnectionOptions>())).Returns(ldapSchemaMock.Object);
        
        var connectionFactoryMock = new Mock<ILdapConnectionFactory>();
        var domainLoaderMock = new Mock<ILdapForestLoader>();
        var suffixes = new List<string>(suffixCount);
        for (var i = 0; i < suffixCount; i++)
            suffixes.Add("suffix" + i);
        
        domainLoaderMock.Setup(x=> x.LoadDomainSuffixes(It.IsAny<ILdapConnection>(), It.IsAny<ILdapSchema>())).Returns(suffixes);
        
        var domainsLoaderProviderMock = new Mock<ILdapForestLoaderProvider>();
        domainsLoaderProviderMock.Setup(x => x.GetTrustedDomainsLoader(It.IsAny<LdapImplementation>())).Returns(() => domainLoaderMock.Object);
        var cacheMock = new Mock<ICacheService>();
        
        var forestService = new LdapForestService(
            ldapSchemaLoaderMock.Object,
            connectionFactoryMock.Object,
            domainsLoaderProviderMock.Object,
            cacheMock.Object,
            NullLogger<LdapForestService>.Instance);
        var options = GetConnectionOptions();
        
        //Act
        var result = forestService.LoadLdapForest(options, false, true);

        //Assert
        Assert.NotNull(result);
        Assert.Single(result);
        var tree = result.First();
        Assert.Equal(ldapSchemaMock.Object, tree.Schema);
        var count = suffixCount + 1;
        Assert.Equal(count, tree.Suffixes.Count);
        domainLoaderMock.Verify(x => x.LoadDomainSuffixes(It.IsAny<ILdapConnection>(), It.IsAny<ILdapSchema>()), Times.Once());
    }
    
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void LoadLdapForest_LoadTrustedDomainsAndLoadSuffixesTrue_ShouldReturnAllDomainsAndSuffixes(int counter)
    {
        //Arrange
        var options = GetConnectionOptions();
        var ldapSchemaLoaderMock = new Mock<ILdapSchemaLoader>();
        var rootSchemaMock = new Mock<ILdapSchema>();
        var namingContext = new DistinguishedName("dc=domain,dc=com");
        rootSchemaMock.Setup(x => x.LdapServerImplementation).Returns(LdapImplementation.Unknown);
        rootSchemaMock.Setup(x => x.NamingContext).Returns(namingContext);
        ldapSchemaLoaderMock.Setup(x => x.Load(It.Is<LdapConnectionOptions>(c => c.ConnectionString == options.ConnectionString))).Returns(rootSchemaMock.Object);
        
        var trustedSchemaMock = new Mock<ILdapSchema>();
        trustedSchemaMock.Setup(x => x.LdapServerImplementation).Returns(LdapImplementation.ActiveDirectory);
        trustedSchemaMock.Setup(x => x.NamingContext).Returns(new DistinguishedName("dc=trusted,dc=domain"));
        ldapSchemaLoaderMock.Setup(x => x.Load(It.Is<LdapConnectionOptions>(c => c.ConnectionString != options.ConnectionString))).Returns(trustedSchemaMock.Object);
        
        var connectionFactoryMock = new Mock<ILdapConnectionFactory>();
        var domainLoaderMock = new Mock<ILdapForestLoader>();
        var suffixes = new List<string>(counter);
        for (var i = 0; i < counter; i++)
            suffixes.Add("suffix" + i);
        var trustedDomains = Enumerable.Repeat(new DistinguishedName("dc=trusted,dc=domain"), counter);
        domainLoaderMock.Setup(x=> x.LoadTrustedDomains(It.IsAny<ILdapConnection>(), It.Is<ILdapSchema>(s => s == rootSchemaMock.Object))).Returns(trustedDomains);
        domainLoaderMock.Setup(x=> x.LoadTrustedDomains(It.IsAny<ILdapConnection>(), It.Is<ILdapSchema>(s => s == trustedSchemaMock.Object))).Returns([]);
        domainLoaderMock.Setup(x=> x.LoadDomainSuffixes(It.IsAny<ILdapConnection>(), It.IsAny<ILdapSchema>())).Returns(suffixes);
        
        var domainsLoaderProviderMock = new Mock<ILdapForestLoaderProvider>();
        domainsLoaderProviderMock.Setup(x => x.GetTrustedDomainsLoader(It.IsAny<LdapImplementation>())).Returns(() => domainLoaderMock.Object);
        var cacheMock = new Mock<ICacheService>();
        
        var forestService = new LdapForestService(
            ldapSchemaLoaderMock.Object,
            connectionFactoryMock.Object,
            domainsLoaderProviderMock.Object,
            cacheMock.Object,
            NullLogger<LdapForestService>.Instance);
        
        //Act
        var result = forestService.LoadLdapForest(options, true, true);

        //Assert
        var expectedEntitiesCount = counter + 1;
        Assert.NotNull(result);
        Assert.Equal(expectedEntitiesCount, result.Count);
        Assert.True(result.All(x => x.Suffixes.Count == expectedEntitiesCount));
        domainLoaderMock.Verify(x => x.LoadDomainSuffixes(It.IsAny<ILdapConnection>(), It.IsAny<ILdapSchema>()), Times.Exactly(expectedEntitiesCount));
        domainLoaderMock.Verify(x => x.LoadTrustedDomains(It.IsAny<ILdapConnection>(), It.IsAny<ILdapSchema>()), Times.Once);
        ldapSchemaLoaderMock.Verify(x => x.Load(It.IsAny<LdapConnectionOptions>()), Times.Exactly(expectedEntitiesCount));
    }

    [Fact]
    public void LoadLdapForest_ShouldLoadFromCache()
    {
        //Arrange
        var ldapSchemaLoaderMock = new Mock<ILdapSchemaLoader>();
        var connectionFactoryMock = new Mock<ILdapConnectionFactory>();
        var domainsLoaderProviderMock = new Mock<ILdapForestLoaderProvider>();
        var cacheMock = new Mock<ICacheService>();
        var key = "forest_url";
        var forest = new List<LdapForestEntry> { new LdapForestEntry(LdapSchemaBuilder.Default) };
        cacheMock.Setup(x => x.TryGetValue(key, out forest)).Returns(true);
        var forestService = new LdapForestService(
            ldapSchemaLoaderMock.Object,
            connectionFactoryMock.Object,
            domainsLoaderProviderMock.Object,
            cacheMock.Object,
            NullLogger<LdapForestService>.Instance);
        var options = GetConnectionOptions();
        
        //Act
        var result = forestService.LoadLdapForest(options, true, true);
        
        //Assert
        Assert.Single(result);
        cacheMock.Verify(x => x.TryGetValue(key, out forest), Times.Once);
    }

    private LdapConnectionOptions GetConnectionOptions() => new(new LdapConnectionString("url"), AuthType.Basic, "name", "password");
}