using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;
using Moq;
using Multifactor.Radius.Adapter.v2.Infrastructure.Features;
using Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline;
using Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline.Steps;

namespace Multifactor.Radius.Adapter.v2.Tests.PipelineTests;

public class BuildPipelineTest
{
    [Fact]
    public void BuildPipeline_ShouldReturnPipelineSteps()
    {
        var config = new PipelineStepsConfiguration("name", PreAuthMode.None, shouldCheckMembership: true);
        
        var cacheMock = new Mock<IMemoryCache>();
        var outVal = new object(); 
        cacheMock.Setup(x => x.CreateEntry(It.IsAny<object>())).Returns(new Entry());
        cacheMock.Setup(x => x.TryGetValue(It.IsAny<string>(), out outVal)).Returns(false);
        
        var builder = new PipelineStepTypesFactory(cacheMock.Object);
        
        var pipeline = builder.GetPipelineStepTypes(config);
        
        var steps = pipeline.Select(x => Activator.CreateInstance(x) as IRadiusPipelineStep).ToArray();
        Assert.NotEmpty(steps);
        Assert.All(steps, Assert.NotNull);
    }

    [Fact]
    public void BuildPipeline_ShouldBuildDefaultPipeline()
    {
        var config = new PipelineStepsConfiguration("name", PreAuthMode.None, shouldCheckMembership: true);
        
        var cacheMock = new Mock<IMemoryCache>();
        var outVal = new object(); 
        cacheMock.Setup(x => x.CreateEntry(It.IsAny<object>())).Returns(new Entry());
        cacheMock.Setup(x => x.TryGetValue(It.IsAny<string>(), out outVal)).Returns(false);
        
        var builder = new PipelineStepTypesFactory(cacheMock.Object);
        
        var pipeline = builder.GetPipelineStepTypes(config);
        var steps = pipeline.Select(x => Activator.CreateInstance(x) as IRadiusPipelineStep);
        
        Assert.Collection<IRadiusPipelineStep?>(
            steps,
            IsType<StatusServerFilteringStep>()!,
            IsType<AccessRequestFilteringStep>()!,
            IsType<ProfileLoadingStep>()!,
            IsType<AccessChallengeStep>()!,
            IsType<FirstFactorStep>()!,
            IsType<CheckingMembershipStep>()!,
            IsType<SecondFactorStep>()!,
            IsType<RequestPostProcessStep>()!);
    }
    
    [Theory]
    [InlineData(PreAuthMode.Otp)]
    [InlineData(PreAuthMode.Push)]
    [InlineData(PreAuthMode.Telegram)]
    public void BuildPipeline_ShouldBuildPreAuthPipeline(PreAuthMode mode)
    {
        var config = new PipelineStepsConfiguration("name", mode, shouldCheckMembership: true);
        
        var cacheMock = new Mock<IMemoryCache>();
        var outVal = new object(); 
        cacheMock.Setup(x => x.CreateEntry(It.IsAny<object>())).Returns(new Entry());
        cacheMock.Setup(x => x.TryGetValue(It.IsAny<string>(), out outVal)).Returns(false);
        
        var builder = new PipelineStepTypesFactory(cacheMock.Object);
        
        var pipeline = builder.GetPipelineStepTypes(config);
        var steps = pipeline.Select(x => Activator.CreateInstance(x) as IRadiusPipelineStep);
        
        Assert.Collection<IRadiusPipelineStep?>(
            steps,
            IsType<StatusServerFilteringStep>()!,
            IsType<AccessRequestFilteringStep>()!,
            IsType<ProfileLoadingStep>()!,
            IsType<AccessChallengeStep>()!,
            IsType<SecondFactorStep>()!,
            IsType<FirstFactorStep>()!,
            IsType<CheckingMembershipStep>()!,
            IsType<RequestPostProcessStep>()!);
    }
    
    [Fact]
    public void BuildPipeline_ShouldBuildPipelineWithoutMembership()
    {
        var config = new PipelineStepsConfiguration("name", PreAuthMode.None, shouldCheckMembership: false);
        
        var cacheMock = new Mock<IMemoryCache>();
        var outVal = new object(); 
        cacheMock.Setup(x => x.CreateEntry(It.IsAny<object>())).Returns(new Entry());
        cacheMock.Setup(x => x.TryGetValue(It.IsAny<string>(), out outVal)).Returns(false);
        
        var builder = new PipelineStepTypesFactory(cacheMock.Object);
        
        var pipeline = builder.GetPipelineStepTypes(config);
        var steps = pipeline.Select(x => Activator.CreateInstance(x) as IRadiusPipelineStep);
        
        Assert.Collection<IRadiusPipelineStep?>(
            steps,
            IsType<StatusServerFilteringStep>()!,
            IsType<AccessRequestFilteringStep>()!,
            IsType<ProfileLoadingStep>()!,
            IsType<AccessChallengeStep>()!,
            IsType<FirstFactorStep>()!,
            IsType<SecondFactorStep>()!,
            IsType<RequestPostProcessStep>()!);
    }

    private Action<IRadiusPipelineStep> IsType<T>()
    {
        return (IRadiusPipelineStep e) => Assert.IsType<T>(e);
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