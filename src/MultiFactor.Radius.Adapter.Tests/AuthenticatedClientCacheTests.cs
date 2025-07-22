using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration.ClientLevel;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration.Features.AuthenticatedClientCacheFeature;
using MultiFactor.Radius.Adapter.Services;

namespace MultiFactor.Radius.Adapter.Tests;

public class AuthenticatedClientCacheTests
{
    [Fact]
    public void NotMemberOfCacheGroups_ShouldReturnFalse()
    {
        var cache = new AuthenticatedClientCache(NullLogger<AuthenticatedClientCache>.Instance);
        var clientConfig = new Mock<IClientConfiguration>();
        var userGroups = new[] { "group3", "group4" };
        var cacheConfig = AuthenticatedClientCacheConfig.Create("00:00:30", true, "group1;group2");
        clientConfig.Setup(x => x.AuthenticationCacheLifetime).Returns(cacheConfig);
        
        var result = cache.TryHitCache("id", "userName", clientConfig.Object, userGroups);
        
        Assert.False(result);
    }
    
    [Fact]
    public void MemberOfCacheGroups_ShouldReturnTrue()
    {
        var cache = new AuthenticatedClientCache(NullLogger<AuthenticatedClientCache>.Instance);
        var clientConfig = new Mock<IClientConfiguration>();
        var userGroups = new[] { "group1", "group3" };
        var cacheConfig = AuthenticatedClientCacheConfig.Create("10:10:00", true, "group1; group2");
        clientConfig.Setup(x => x.AuthenticationCacheLifetime).Returns(cacheConfig);
        clientConfig.Setup(x => x.Name).Returns("configName");
        cache.SetCache("id", "userName", clientConfig.Object);
        
        var result = cache.TryHitCache("id", "userName", clientConfig.Object, userGroups);
        
        Assert.True(result);
    }
    
    [Fact]
    public void NoCacheGroups_ShouldReturnTrue()
    {
        var cache = new AuthenticatedClientCache(NullLogger<AuthenticatedClientCache>.Instance);
        var clientConfig = new Mock<IClientConfiguration>();
        var userGroups = new[] { "group1", "group2" };
        var cacheConfig = AuthenticatedClientCacheConfig.Create("10:10:00", true);
        clientConfig.Setup(x => x.AuthenticationCacheLifetime).Returns(cacheConfig);
        clientConfig.Setup(x => x.Name).Returns("configName");
        cache.SetCache("id", "userName", clientConfig.Object);
        
        var result = cache.TryHitCache("id", "userName", clientConfig.Object, userGroups);
        
        Assert.True(result);
    }
    
    [Fact]
    public void NoUserGroups_ShouldReturnFalse()
    {
        var cache = new AuthenticatedClientCache(NullLogger<AuthenticatedClientCache>.Instance);
        var clientConfig = new Mock<IClientConfiguration>();
        var userGroups = Array.Empty<string>();
        var cacheConfig = AuthenticatedClientCacheConfig.Create("10:10:00", true, "group1; group2");
        clientConfig.Setup(x => x.AuthenticationCacheLifetime).Returns(cacheConfig);
        clientConfig.Setup(x => x.Name).Returns("configName");
        cache.SetCache("id", "userName", clientConfig.Object);
        
        var result = cache.TryHitCache("id", "userName", clientConfig.Object, userGroups);
        
        Assert.False(result);
    }
}