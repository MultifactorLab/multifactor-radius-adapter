using System.Net;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using MultiFactor.Radius.Adapter.Core.Framework.Context;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration.ClientLevel;
using MultiFactor.Radius.Adapter.Server;
using MultiFactor.Radius.Adapter.Server.Pipeline.AccessChallenge;
using MultiFactor.Radius.Adapter.Services;
using MultiFactor.Radius.Adapter.Services.Ldap;
using MultiFactor.Radius.Adapter.Services.MultiFactorApi;
using MultiFactor.Radius.Adapter.Tests.Fixtures;
using MultiFactor.Radius.Adapter.Tests.Fixtures.ConfigLoading;
using MultiFactor.Radius.Adapter.Tests.Fixtures.Radius;

namespace MultiFactor.Radius.Adapter.Tests;

public class ChallengeProcessorProviderTests
{
    [Fact]
    public void GetByType_ShouldReturnChangePasswordChallengeProcessor()
    {
        var host = TestHostFactory.CreateHost(builder =>
        {
            builder.Services.Configure<TestConfigProviderOptions>(x =>
            {
                x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-single.config");
            });
        });
        
        var provider = host.Service<IChallengeProcessorProvider>();
        var processor = provider.GetChallengeProcessorByType(ChallengeType.PasswordChange);
        
        Assert.NotNull(processor);
        Assert.IsType<ChangePasswordChallengeProcessor>(processor);
    }
    
    [Fact]
    public void GetByType_ShouldReturnSecondFactorChallengeProcessor()
    {
        var host = TestHostFactory.CreateHost(builder =>
        {
            builder.Services.Configure<TestConfigProviderOptions>(x =>
            {
                x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-single.config");
            });
        });
        
        var provider = host.Service<IChallengeProcessorProvider>();
        var processor = provider.GetChallengeProcessorByType(ChallengeType.SecondFactor);
        
        Assert.NotNull(processor);
        Assert.IsType<SecondFactorChallengeProcessor>(processor);
    }
    
    [Fact]
    public void GetByIdentifier_ChangePasswordIdentifierExists_ShouldReturnChangePasswordChallengeProcessor()
    {
        var processors = new List<IChallengeProcessor>();
        var memCache = new Mock<IMemoryCache>();
        var dataProtectionProviderMock = new Mock<IDataProtectionProvider>();
        var dataProtectionServiceMock = new DataProtectionService(dataProtectionProviderMock.Object);
        var outVal = new object();
        memCache.Setup(m => m.TryGetValue(It.IsAny<object>(), out outVal)).Returns(true);
        processors.Add(new ChangePasswordChallengeProcessor(memCache.Object, new Mock<ILdapService>().Object, dataProtectionServiceMock, new Mock<ILogger<ChangePasswordChallengeProcessor>>().Object));
        processors.Add(new SecondFactorChallengeProcessor(new Mock<IMultifactorApiAdapter>().Object, new Mock<ILogger<SecondFactorChallengeProcessor>>().Object));
        
        var provider = new ChallengeProcessorProvider(processors);
        var processor = provider.GetChallengeProcessorForIdentifier(new ChallengeIdentifier("name", "requestId"));
        
        Assert.NotNull(processor);
        Assert.IsType<ChangePasswordChallengeProcessor>(processor);
    }
    
    [Fact]
    public void GetByIdentifier_SecondFactorIdentifierExists_ShouldReturnSecondFactorChallengeProcessor()
    {
        var host = TestHostFactory.CreateHost(builder =>
        {
            builder.Services.Configure<TestConfigProviderOptions>(x =>
            {
                x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-single.config");
            });
        });
        var client = new ClientConfiguration("cli_config", "rds", AuthenticationSource.None, "key", "secret");
        var context = new RadiusContext(RadiusPacketFactory.AccessRequest(), client, new Mock<IServiceProvider>().Object)
        {
            RemoteEndpoint = new IPEndPoint(IPAddress.Any, 636)
        };
        context.SetMessageState("state");
        
        var provider = host.Service<IChallengeProcessorProvider>();
        var processor = provider.GetChallengeProcessorByType(ChallengeType.SecondFactor);
        var identifier = processor.AddChallengeContext(context);
        
        processor = provider.GetChallengeProcessorForIdentifier(identifier);
        Assert.NotNull(processor);
        Assert.IsType<SecondFactorChallengeProcessor>(processor);
    }

    [Fact]
    public void GetByType_NoneType_ShouldReturnNull()
    {
        var host = TestHostFactory.CreateHost(builder =>
        {
            builder.Services.Configure<TestConfigProviderOptions>(x =>
            {
                x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-single.config");
            });
        });
        
        var provider = host.Service<IChallengeProcessorProvider>();
        var processor = provider.GetChallengeProcessorByType(ChallengeType.None);
        
        Assert.Null(processor);
    }
    
    [Fact]
    public void GetByIdentifier_NoIdentifier_ShouldReturnNull()
    {
        var host = TestHostFactory.CreateHost(builder =>
        {
            builder.Services.Configure<TestConfigProviderOptions>(x =>
            {
                x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-single.config");
            });
        });
        
        var provider = host.Service<IChallengeProcessorProvider>();
        var identifier = new ChallengeIdentifier("name", "requestId");
        var processor = provider.GetChallengeProcessorForIdentifier(identifier);
        
        Assert.Null(processor);
    }

    [Fact]
    public void GetByIdentifier_IdentifierIsNull_ShouldThrowNullReferenceException()
    {
        var host = TestHostFactory.CreateHost(builder =>
        {
            builder.Services.Configure<TestConfigProviderOptions>(x =>
            {
                x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-single.config");
            });
        });
        
        var provider = host.Service<IChallengeProcessorProvider>();

        Assert.Throws<ArgumentNullException>(() => provider.GetChallengeProcessorForIdentifier(null));
    }
}