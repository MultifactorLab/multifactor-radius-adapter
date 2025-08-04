using MultiFactor.Radius.Adapter.Infrastructure.Configuration.Features.AuthenticatedClientCacheFeature;

namespace MultiFactor.Radius.Adapter.Tests;

public class AuthenticatedClientCacheConfigTests
{
    [Fact]
    public void ShouldCreateConfigWithoutGroups()
    {
        var config = AuthenticatedClientCacheConfig.Create("10:10:00", true);
        
        Assert.NotNull(config);
        Assert.Empty(config.AuthenticationCacheGroups);
    }

    [Fact]
    public void SingleCacheGroup_ShouldCreateConfigWithGroup()
    {
        var config = AuthenticatedClientCacheConfig.Create("10:10:00", true, "group");
        
        Assert.NotNull(config);
        Assert.Single(config.AuthenticationCacheGroups);
        Assert.Equal("group", config.AuthenticationCacheGroups.First());
    }
    
    [Fact]
    public void MultipleCacheGroups_ShouldCreateConfigWithGroups()
    {
        var config = AuthenticatedClientCacheConfig.Create("10:10:00", true, "Group1;  GrouP2;     grOUp3");
        
        Assert.NotNull(config);
        Assert.Equal(3, config.AuthenticationCacheGroups.Count);
        Assert.True(config.AuthenticationCacheGroups.SequenceEqual(["group1", "group2", "group3"]));
    }
}