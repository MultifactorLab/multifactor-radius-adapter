using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;
using Moq;
using Multifactor.Radius.Adapter.v2.Core.Auth.PreAuthMode;
using Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline;
using Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline.Steps;
using Multifactor.Radius.Adapter.v2.Services.Cache;

namespace Multifactor.Radius.Adapter.v2.Tests.PipelineTests;

public class PipelineConfigurationFactoryTests
{
    [Fact]
    public void CreatePipelineConfiguration_ShouldReturnConfiguration()
    {
        var config = new PipelineStepsConfiguration("name", PreAuthMode.None);
        
        var cache = new Mock<ICacheService>();
        var outVal = new PipelineConfiguration([]);
        cache.Setup(x => x.TryGetValue(It.IsAny<string>(), out outVal)).Returns(false);
        
        var factory = new PipelineConfigurationFactory(cache.Object);
        
        var pipelineConfiguration = factory.CreatePipelineConfiguration(config);
        
        Assert.NotNull(pipelineConfiguration);
        Assert.NotEmpty(pipelineConfiguration.PipelineStepsTypes);
        Assert.All(pipelineConfiguration.PipelineStepsTypes, Assert.NotNull);
    }

    [Fact]
    public void BuildPipelineConfiguration_ShouldReturnDefaultConfig()
    {
        var config = new PipelineStepsConfiguration("name", PreAuthMode.None, hasLdapServers: true);
        
        var cache = new Mock<ICacheService>();
        var outVal = new PipelineConfiguration([]);
        cache.Setup(x => x.TryGetValue(It.IsAny<string>(), out outVal)).Returns(false);
        
        var pipelineConfigurationFactory = new PipelineConfigurationFactory(cache.Object);
        
        var pipelineConfiguration = pipelineConfigurationFactory.CreatePipelineConfiguration(config);
        Assert.Collection(
            pipelineConfiguration.PipelineStepsTypes,
            e => Assert.True(typeof(StatusServerFilteringStep).IsAssignableFrom(e)),
            e => Assert.True(typeof(IpWhiteListStep).IsAssignableFrom(e)),
            e => Assert.True(typeof(AccessRequestFilteringStep).IsAssignableFrom(e)),
            e => Assert.True(typeof(UserNameValidationStep).IsAssignableFrom(e)),
            e => Assert.True(typeof(LdapSchemaLoadingStep).IsAssignableFrom(e)),
            e => Assert.True(typeof(ProfileLoadingStep).IsAssignableFrom(e)),
            e => Assert.True(typeof(AccessGroupsCheckingStep).IsAssignableFrom(e)),
            e => Assert.True(typeof(AccessChallengeStep).IsAssignableFrom(e)),
            e => Assert.True(typeof(FirstFactorStep).IsAssignableFrom(e)),
            e => Assert.True(typeof(SecondFactorStep).IsAssignableFrom(e)));
    }
    
    [Fact]
    public void BuildPipelineConfiguration_GroupLoading_ShouldReturnConfig()
    {
        var config = new PipelineStepsConfiguration("name", PreAuthMode.None, true, true);
        
        var cache = new Mock<ICacheService>();
        var outVal = new PipelineConfiguration([]);
        cache.Setup(x => x.TryGetValue(It.IsAny<string>(), out outVal)).Returns(false);
        
        var pipelineConfigurationFactory = new PipelineConfigurationFactory(cache.Object);
        
        var pipelineConfiguration = pipelineConfigurationFactory.CreatePipelineConfiguration(config);
        Assert.Collection(
            pipelineConfiguration.PipelineStepsTypes,
            e => Assert.True(typeof(StatusServerFilteringStep).IsAssignableFrom(e)),
            e => Assert.True(typeof(IpWhiteListStep).IsAssignableFrom(e)),
            e => Assert.True(typeof(AccessRequestFilteringStep).IsAssignableFrom(e)),
            e => Assert.True(typeof(UserNameValidationStep).IsAssignableFrom(e)),
            e => Assert.True(typeof(LdapSchemaLoadingStep).IsAssignableFrom(e)),
            e => Assert.True(typeof(ProfileLoadingStep).IsAssignableFrom(e)),
            e => Assert.True(typeof(AccessGroupsCheckingStep).IsAssignableFrom(e)),
            e => Assert.True(typeof(AccessChallengeStep).IsAssignableFrom(e)),
            e => Assert.True(typeof(FirstFactorStep).IsAssignableFrom(e)),
            e => Assert.True(typeof(SecondFactorStep).IsAssignableFrom(e)),
            e => Assert.True(typeof(UserGroupLoadingStep).IsAssignableFrom(e)));
    }
    
    [Theory]
    [InlineData(PreAuthMode.Otp)]
    [InlineData(PreAuthMode.Any)]
    public void BuildPipelineConfiguration_ShouldReturnPreAuthConfiguration(PreAuthMode mode)
    {
        var config = new PipelineStepsConfiguration("name", mode, hasLdapServers: true);
        
        var cache = new Mock<ICacheService>();
        var outVal = new PipelineConfiguration([]);
        cache.Setup(x => x.TryGetValue(It.IsAny<string>(), out outVal)).Returns(false);
        
        var pipelineConfigurationFactory = new PipelineConfigurationFactory(cache.Object);
        
        var pipelineConfiguration = pipelineConfigurationFactory.CreatePipelineConfiguration(config);
        Assert.Collection(
            pipelineConfiguration.PipelineStepsTypes,
            e => Assert.True(typeof(StatusServerFilteringStep).IsAssignableFrom(e)),
            e => Assert.True(typeof(IpWhiteListStep).IsAssignableFrom(e)),
            e => Assert.True(typeof(AccessRequestFilteringStep).IsAssignableFrom(e)),
            e => Assert.True(typeof(UserNameValidationStep).IsAssignableFrom(e)),
            e => Assert.True(typeof(LdapSchemaLoadingStep).IsAssignableFrom(e)),
            e => Assert.True(typeof(ProfileLoadingStep).IsAssignableFrom(e)),
            e => Assert.True(typeof(AccessGroupsCheckingStep).IsAssignableFrom(e)),
            e => Assert.True(typeof(AccessChallengeStep).IsAssignableFrom(e)),
            e => Assert.True(typeof(PreAuthCheckStep).IsAssignableFrom(e)),
            e => Assert.True(typeof(SecondFactorStep).IsAssignableFrom(e)),
            e => Assert.True(typeof(PreAuthPostCheck).IsAssignableFrom(e)),
            e => Assert.True(typeof(FirstFactorStep).IsAssignableFrom(e)));
    }
    
    [Fact]
    public void BuildPipelineConfiguration_ShouldReturnConfigurationWithoutMembership()
    {
        var config = new PipelineStepsConfiguration("name", PreAuthMode.None, hasLdapServers: true);
        
        var cache = new Mock<ICacheService>();
        var outVal = new PipelineConfiguration([]); 
        cache.Setup(x => x.TryGetValue(It.IsAny<string>(), out outVal)).Returns(false);
        
        var pipelineConfigurationFactory = new PipelineConfigurationFactory(cache.Object);
        
        var pipelineConfiguration = pipelineConfigurationFactory.CreatePipelineConfiguration(config);
        Assert.Collection(
            pipelineConfiguration.PipelineStepsTypes,
            e => Assert.True(typeof(StatusServerFilteringStep).IsAssignableFrom(e)),
            e => Assert.True(typeof(IpWhiteListStep).IsAssignableFrom(e)),
            e => Assert.True(typeof(AccessRequestFilteringStep).IsAssignableFrom(e)),
            e => Assert.True(typeof(UserNameValidationStep).IsAssignableFrom(e)),
            e => Assert.True(typeof(LdapSchemaLoadingStep).IsAssignableFrom(e)),
            e => Assert.True(typeof(ProfileLoadingStep).IsAssignableFrom(e)),
            e => Assert.True(typeof(AccessGroupsCheckingStep).IsAssignableFrom(e)),
            e => Assert.True(typeof(AccessChallengeStep).IsAssignableFrom(e)),
            e => Assert.True(typeof(FirstFactorStep).IsAssignableFrom(e)),
            e => Assert.True(typeof(SecondFactorStep).IsAssignableFrom(e)));
    }

    [Fact]
    public void BuildPipelineConfiguration_NoLdapServers_ShouldReturnConfig()
    {
        var config = new PipelineStepsConfiguration("name", PreAuthMode.None, hasLdapServers: false);
        
        var cache = new Mock<ICacheService>();
        var outVal = new PipelineConfiguration([]); 
        cache.Setup(x => x.TryGetValue(It.IsAny<string>(), out outVal)).Returns(false);
        
        var pipelineConfigurationFactory = new PipelineConfigurationFactory(cache.Object);
        
        var pipelineConfiguration = pipelineConfigurationFactory.CreatePipelineConfiguration(config);
        Assert.Collection(
            pipelineConfiguration.PipelineStepsTypes,
            e => Assert.True(typeof(StatusServerFilteringStep).IsAssignableFrom(e)),
            e => Assert.True(typeof(IpWhiteListStep).IsAssignableFrom(e)),
            e => Assert.True(typeof(AccessRequestFilteringStep).IsAssignableFrom(e)),
            e => Assert.True(typeof(AccessChallengeStep).IsAssignableFrom(e)),
            e => Assert.True(typeof(FirstFactorStep).IsAssignableFrom(e)),
            e => Assert.True(typeof(SecondFactorStep).IsAssignableFrom(e)));
    }
    
    
    [Theory]
    [InlineData(PreAuthMode.Otp)]
    [InlineData(PreAuthMode.Any)]
    public void BuildPipelineConfiguration_NoLdapServersWithPreAuth_ShouldReturnConfig(PreAuthMode mode)
    {
        var config = new PipelineStepsConfiguration("name", mode, hasLdapServers: false);
        
        var cache = new Mock<ICacheService>();
        var outVal = new PipelineConfiguration([]); 
        cache.Setup(x => x.TryGetValue(It.IsAny<string>(), out outVal)).Returns(false);
        
        var pipelineConfigurationFactory = new PipelineConfigurationFactory(cache.Object);
        
        var pipelineConfiguration = pipelineConfigurationFactory.CreatePipelineConfiguration(config);
        Assert.Collection(
            pipelineConfiguration.PipelineStepsTypes,
            e => Assert.True(typeof(StatusServerFilteringStep).IsAssignableFrom(e)),
            e => Assert.True(typeof(IpWhiteListStep).IsAssignableFrom(e)),
            e => Assert.True(typeof(AccessRequestFilteringStep).IsAssignableFrom(e)),
            e => Assert.True(typeof(AccessChallengeStep).IsAssignableFrom(e)),
            e => Assert.True(typeof(PreAuthCheckStep).IsAssignableFrom(e)),
            e => Assert.True(typeof(SecondFactorStep).IsAssignableFrom(e)),
            e => Assert.True(typeof(PreAuthPostCheck).IsAssignableFrom(e)),
            e => Assert.True(typeof(FirstFactorStep).IsAssignableFrom(e)));
    }

    public class Entry : ICacheEntry
    {
        public void Dispose()
        {
        }

        public object Key { get; }
        public object? Value { get; set; }
        public DateTimeOffset? AbsoluteExpiration { get; set; }
        public TimeSpan? AbsoluteExpirationRelativeToNow { get; set; }
        public TimeSpan? SlidingExpiration { get; set; }
        public IList<IChangeToken> ExpirationTokens { get; }
        public IList<PostEvictionCallbackRegistration> PostEvictionCallbacks { get; }
        public CacheItemPriority Priority { get; set; }
        public long? Size { get; set; }
    }
}